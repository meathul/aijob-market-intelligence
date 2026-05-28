export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type AuthResponse = {
  accessToken?: string;
  token?: string;
  expiresAtUtc?: string;
  email?: string;
  roles?: string[];
};
