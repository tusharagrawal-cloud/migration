// Mirrors the Admin auth API response shapes exactly (One77.Core.Admin /
// One77.Api.DevHost.Admin / One77.Api.WebApi48.Auth — Milestone 10).

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  access_token: string;
  token_type: string;
  admin_user_id: number;
  email: string;
  name: string;
}

export interface AdminMe {
  admin_user_id: number;
  email: string;
  name: string;
}
