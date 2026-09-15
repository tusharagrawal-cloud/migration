using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace One77.Core.Match
{
    /// <summary>
    /// Admin Match operations: reproduces backend/match_routes.py's admin
    /// surface (airguns list, relationships, candidates, upsert, patch,
    /// delete, bulk, summary/completeness) against the SQL Server schema.
    /// </summary>
    public sealed class MatchAdminService
    {
        private readonly MatchRepository _repository;

        public MatchAdminService(MatchRepository repository)
        {
            _repository = repository;
        }

        // ---------- Airguns / completeness ----------

        public async Task<List<MatchAirgunSummaryItem>> GetAirgunsAsync()
        {
            // Reference system lists all airguns regardless of product
            // status (admin tooling) — same here: activeOnly:false.
            var airguns = await _repository.GetProductRefsByCategoryAsync(MatchCategory.Airgun, activeOnly: false);
            var result = new List<MatchAirgunSummaryItem>();
            foreach (var a in airguns)
            {
                result.Add(new MatchAirgunSummaryItem
                {
                    ShopifyProductId = a.ShopifyProductId,
                    Calibre = a.Calibre,
                    PowerplantType = a.PowerplantType,
                    Counts = await ComputeCompletenessAsync(a.ShopifyProductId)
                });
            }
            return result;
        }

        public Task<MatchCompleteness> GetCompletenessAsync(string shopifyProductId)
        {
            return ComputeCompletenessAsync(shopifyProductId);
        }

        /// <summary>Mirrors the reference _completeness() exactly.</summary>
        private async Task<MatchCompleteness> ComputeCompletenessAsync(string sourceShopifyProductId)
        {
            var rels = await _repository.GetActiveRelationshipsForSourceAsync(sourceShopifyProductId, null);

            int compatPellets = 0, recPellets = 0, compatAcc = 0, recAcc = 0;
            foreach (var r in rels)
            {
                if (r.Status == MatchStatus.NotRecommended)
                {
                    continue;
                }
                var isPellet = r.TargetCategory == MatchCategory.Pellet;
                if (r.Status == MatchStatus.Recommended)
                {
                    if (isPellet) recPellets++; else recAcc++;
                }
                if (r.Status == MatchStatus.Compatible || r.Status == MatchStatus.Recommended)
                {
                    if (isPellet) compatPellets++; else compatAcc++;
                }
            }

            var hasPellet = compatPellets > 0 && recPellets > 0;
            var hasAcc = compatAcc > 0;
            var total = compatPellets + recPellets + compatAcc + recAcc;

            string state;
            if (total == 0) state = "no_matches";
            else if (hasPellet && hasAcc) state = "complete";
            else state = "needs_review";

            return new MatchCompleteness
            {
                CompatPellets = compatPellets,
                RecPellets = recPellets,
                CompatAcc = compatAcc,
                RecAcc = recAcc,
                State = state
            };
        }

        // ---------- Relationships / candidates ----------

        public async Task<List<MatchRelationshipView>> GetRelationshipsAsync(string sourceShopifyProductId, string targetCategory)
        {
            var rels = await _repository.GetActiveRelationshipsForSourceAsync(sourceShopifyProductId, targetCategory);
            if (rels.Count == 0)
            {
                return new List<MatchRelationshipView>();
            }

            var targets = await _repository.GetProductRefsByShopifyIdsAsync(rels.Select(r => r.TargetShopifyProductId), activeOnly: false);
            var targetsById = targets.ToDictionary(t => t.ShopifyProductId);

            return rels.Select(r => ToView(r, targetsById.TryGetValue(r.TargetShopifyProductId, out var t) ? t : null)).ToList();
        }

        public async Task<List<MatchCandidateItem>> GetCandidatesAsync(string sourceShopifyProductId, string targetCategory, string calibre, decimal? weight)
        {
            if (targetCategory != MatchCategory.Pellet && targetCategory != MatchCategory.Accessory)
            {
                throw new MatchValidationException("target_category must be 'pellet' or 'accessory'.");
            }

            // Admin candidate search intentionally does not filter by
            // IsActive — mirrors the reference system, which searches all
            // products in the category regardless of product status so an
            // admin can link to not-yet-published products too.
            var candidates = await _repository.GetProductRefsByCategoryAsync(targetCategory, activeOnly: false);

            if (!string.IsNullOrEmpty(calibre))
            {
                candidates = candidates.Where(c => c.Calibre == calibre).ToList();
            }
            if (weight.HasValue)
            {
                candidates = candidates.Where(c => c.WeightGrains.HasValue && System.Math.Abs(c.WeightGrains.Value - weight.Value) < 1.5m).ToList();
            }

            var existingRels = await _repository.GetActiveRelationshipsForSourceAsync(sourceShopifyProductId, null);
            var existingByTarget = existingRels.ToDictionary(r => r.TargetShopifyProductId);

            return candidates.Select(c => new MatchCandidateItem
            {
                Product = c,
                CurrentRelationship = existingByTarget.TryGetValue(c.ShopifyProductId, out var rel) ? ToView(rel, null) : null
            }).ToList();
        }

        // ---------- Upsert / patch / delete ----------

        public async Task<MatchRelationshipView> UpsertRelationshipAsync(
            string sourceShopifyProductId, string targetShopifyProductId, string status,
            int? priority, List<string> useCases, string reason, string adminNotes, bool calibreOverride)
        {
            if (!MatchStatus.IsValid(status))
            {
                throw new MatchValidationException("Please choose Compatible, Recommended or Not Recommended.");
            }

            var (source, target) = await LoadPairAsync(sourceShopifyProductId, targetShopifyProductId);

            if (status == MatchStatus.Recommended && IsCalibreMismatch(source, target) && !calibreOverride)
            {
                throw new MatchValidationException("Calibre does not match this airgun. Confirm to save anyway.");
            }

            var effectivePriority = NormalizePriorityForUpsert(status, priority);
            var existingId = await _repository.FindRelationshipIdByPairAsync(sourceShopifyProductId, targetShopifyProductId);

            int relationshipId;
            if (existingId.HasValue)
            {
                relationshipId = existingId.Value;
                await _repository.UpdateRelationshipAsync(new MatchRelationship
                {
                    Id = relationshipId,
                    TargetCategory = target.Category,
                    Status = status,
                    Priority = effectivePriority,
                    Reason = (reason ?? "").Trim(),
                    AdminNotes = (adminNotes ?? "").Trim(),
                    CalibreOverride = calibreOverride,
                    IsActive = true
                });
            }
            else
            {
                relationshipId = await _repository.InsertRelationshipAsync(new MatchRelationship
                {
                    SourceShopifyProductId = sourceShopifyProductId,
                    TargetShopifyProductId = targetShopifyProductId,
                    TargetCategory = target.Category,
                    Status = status,
                    Priority = effectivePriority,
                    Reason = (reason ?? "").Trim(),
                    AdminNotes = (adminNotes ?? "").Trim(),
                    CalibreOverride = calibreOverride,
                    IsActive = true
                });
            }

            await _repository.ReplaceRelationshipUseCasesAsync(relationshipId, useCases ?? new List<string>());

            var saved = await _repository.GetRelationshipByIdAsync(relationshipId);
            return ToView(saved, target);
        }

        public async Task<MatchRelationshipView> PatchRelationshipAsync(
            int id, string status, int? priority, List<string> useCases, string reason, string adminNotes, bool? calibreOverride, bool? active)
        {
            var current = await _repository.GetRelationshipByIdAsync(id);
            if (current == null)
            {
                throw new MatchNotFoundException("Relationship not found.");
            }

            if (status != null && !MatchStatus.IsValid(status))
            {
                throw new MatchValidationException("Invalid status.");
            }
            var effectiveStatus = status ?? current.Status;

            // The reference PATCH endpoint only forces Priority=null when
            // Status is changed away from "recommended" in the SAME call; it
            // does not otherwise validate Priority at all, which would let an
            // invalid value reach our DB's CHECK constraint as a raw 500.
            // Closing that gap here (documented in MATCH_PARITY_REPORT.md) —
            // this is a fix, not a new rule.
            if (priority.HasValue)
            {
                if (priority.Value != 1 && priority.Value != 2 && priority.Value != 3)
                {
                    throw new MatchValidationException("Invalid priority.");
                }
                if (effectiveStatus != MatchStatus.Recommended)
                {
                    throw new MatchValidationException("Priority is only valid when status is recommended.");
                }
            }

            int? effectivePriority;
            if (status != null && effectiveStatus != MatchStatus.Recommended)
            {
                effectivePriority = null;
            }
            else if (priority.HasValue)
            {
                effectivePriority = priority.Value;
            }
            else
            {
                effectivePriority = current.Priority;
            }

            var updated = new MatchRelationship
            {
                Id = current.Id,
                TargetCategory = current.TargetCategory,
                Status = effectiveStatus,
                Priority = effectivePriority,
                Reason = reason != null ? reason.Trim() : current.Reason,
                AdminNotes = adminNotes != null ? adminNotes.Trim() : current.AdminNotes,
                CalibreOverride = calibreOverride ?? current.CalibreOverride,
                IsActive = active ?? current.IsActive
            };

            await _repository.UpdateRelationshipAsync(updated);
            if (useCases != null)
            {
                await _repository.ReplaceRelationshipUseCasesAsync(id, useCases);
            }

            var saved = await _repository.GetRelationshipByIdAsync(id);
            var target = await _repository.GetProductRefByShopifyIdAsync(saved.TargetShopifyProductId);
            return ToView(saved, target);
        }

        public async Task DeleteRelationshipAsync(int id)
        {
            var current = await _repository.GetRelationshipByIdAsync(id);
            if (current == null)
            {
                throw new MatchNotFoundException("Relationship not found.");
            }
            await _repository.SetRelationshipActiveAsync(id, false);
        }

        // ---------- Bulk ----------

        public async Task<MatchBulkResult> BulkMarkAsync(string sourceShopifyProductId, List<string> targetShopifyProductIds, string status)
        {
            if (!MatchStatus.IsValid(status))
            {
                throw new MatchValidationException("Invalid status.");
            }

            // Reference bulk_mark() only checks the source product exists —
            // unlike single upsert, it does NOT require Category=='airgun'.
            // Preserved exactly (documented in MATCH_PARITY_REPORT.md).
            var source = await _repository.GetProductRefByShopifyIdAsync(sourceShopifyProductId);
            if (source == null)
            {
                throw new MatchValidationException("Airgun not found.");
            }

            int created = 0, updated = 0, skippedCalibre = 0;

            foreach (var targetId in targetShopifyProductIds ?? new List<string>())
            {
                if (targetId == sourceShopifyProductId)
                {
                    continue;
                }
                var target = await _repository.GetProductRefByShopifyIdAsync(targetId);
                if (target == null || (target.Category != MatchCategory.Pellet && target.Category != MatchCategory.Accessory))
                {
                    continue;
                }
                if (status == MatchStatus.Recommended && IsCalibreMismatch(source, target))
                {
                    skippedCalibre++;
                    continue;
                }

                var priority = status == MatchStatus.Recommended ? 2 : (int?)null;
                var existingId = await _repository.FindRelationshipIdByPairAsync(sourceShopifyProductId, targetId);
                if (existingId.HasValue)
                {
                    await _repository.UpdateRelationshipForBulkAsync(existingId.Value, target.Category, status, priority);
                    updated++;
                }
                else
                {
                    var newId = await _repository.InsertRelationshipAsync(new MatchRelationship
                    {
                        SourceShopifyProductId = sourceShopifyProductId,
                        TargetShopifyProductId = targetId,
                        TargetCategory = target.Category,
                        Status = status,
                        Priority = priority,
                        Reason = "",
                        AdminNotes = "",
                        CalibreOverride = false,
                        IsActive = true
                    });
                    await _repository.ReplaceRelationshipUseCasesAsync(newId, new List<string>());
                    created++;
                }
            }

            return new MatchBulkResult { Created = created, Updated = updated, SkippedCalibre = skippedCalibre };
        }

        // ---------- Shared validation helpers (mirror _load_pair / _calibre_mismatch) ----------

        private async Task<(MatchProductRef Source, MatchProductRef Target)> LoadPairAsync(string sourceId, string targetId)
        {
            var source = await _repository.GetProductRefByShopifyIdAsync(sourceId);
            if (source == null || source.Category != MatchCategory.Airgun)
            {
                throw new MatchValidationException("Please choose a valid airgun.");
            }
            var target = await _repository.GetProductRefByShopifyIdAsync(targetId);
            if (target == null)
            {
                throw new MatchValidationException("Please choose a valid target product.");
            }
            if (target.Category != MatchCategory.Pellet && target.Category != MatchCategory.Accessory)
            {
                throw new MatchValidationException("Target must be a Pellet or Accessory.");
            }
            if (sourceId == targetId)
            {
                throw new MatchValidationException("A product cannot be matched to itself.");
            }
            return (source, target);
        }

        /// <summary>
        /// Only ever applies to pellet targets. Missing calibre on either
        /// side means "no mismatch detected" (guard passes) — this is the
        /// reference system's actual behavior (no default applied here,
        /// unlike the public derived resolver's DefaultCalibre helper).
        /// </summary>
        private static bool IsCalibreMismatch(MatchProductRef source, MatchProductRef target)
        {
            if (target.Category != MatchCategory.Pellet)
            {
                return false;
            }
            return !string.IsNullOrEmpty(source.Calibre) && !string.IsNullOrEmpty(target.Calibre) && source.Calibre != target.Calibre;
        }

        /// <summary>
        /// Mirrors `priority = body.priority or 2; if status != recommended: priority = None
        /// elif priority not in (1,2,3): priority = 2` — any missing/invalid
        /// value silently normalizes to 2 rather than being rejected; this
        /// endpoint never returns 400 for priority.
        /// </summary>
        private static int? NormalizePriorityForUpsert(string status, int? requestedPriority)
        {
            if (status != MatchStatus.Recommended)
            {
                return null;
            }
            if (requestedPriority == 1 || requestedPriority == 2 || requestedPriority == 3)
            {
                return requestedPriority;
            }
            return 2;
        }

        private static MatchRelationshipView ToView(MatchRelationship r, MatchProductRef target)
        {
            return new MatchRelationshipView
            {
                Id = r.Id,
                SourceShopifyProductId = r.SourceShopifyProductId,
                TargetShopifyProductId = r.TargetShopifyProductId,
                TargetCategory = r.TargetCategory,
                Status = r.Status,
                Priority = r.Priority,
                PriorityLabel = MatchPriorityLabel.For(r.Priority),
                Reason = r.Reason,
                AdminNotes = r.AdminNotes,
                CalibreOverride = r.CalibreOverride,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                UseCases = r.UseCases ?? new List<string>(),
                Target = target
            };
        }
    }
}
