# Confirm Password + Forgot Password Flow

Add a "Confirm Password" field to the registration form and implement a complete "Forgot Password" flow with email OTP verification and password reset.

## Proposed Changes

### Backend (ClinicApi)

---

#### [MODIFY] AuthController.cs
Add two new endpoints:
- `POST /api/auth/forgot-password` — Accepts `{ email }`, checks user exists, generates OTP, sends it via email. Returns success message.
- `POST /api/auth/reset-password` — Accepts `{ email, code, newPassword }`, verifies OTP, updates the user's `PasswordHash` with the new BCrypt-hashed password.

#### [MODIFY] AuthDtos.cs
Add new DTOs:
- `ForgotPasswordRequest` — `{ Email }`
- `ResetPasswordRequest` — `{ Email, Code, NewPassword }`

---

### Frontend (Clinic - Angular)

---

#### [MODIFY] register.component.ts
- Add `confirmPassword` form control with a custom validator that checks it matches `password`.
- Add `showConfirmPassword` signal for visibility toggle.

#### [MODIFY] register.component.html
- Replace the Password + Phone row with a Password + Confirm Password row (2-column grid).
- Move Phone Number field to its own row below.
- Add a "Confirm Password" input with toggle eye icon and mismatch validation error.

#### [MODIFY] login.component.ts
- Add `forgotPasswordMode` signal and related state signals (`forgotEmail`, `forgotOtpSent`, `forgotOtpInputs`, `forgotNewPassword`, `forgotConfirmPassword`, `forgotCountdown`, `showForgotNewPassword`).
- Add methods: `startForgotPassword()`, `sendForgotOtp()`, `resetPassword()`, `cancelForgotPassword()`.

#### [MODIFY] login.component.html
- Add a "Forgot Password?" link below the password input field (only visible in password mode).
- Add a complete forgot-password UI section with 3 stages: enter email → enter OTP → enter new password + confirm.

#### [MODIFY] auth.service.ts
- Add `forgotPassword(email)` method → calls `POST /api/auth/forgot-password`.
- Add `resetPassword(email, code, newPassword)` method → calls `POST /api/auth/reset-password`.

#### [MODIFY] language.service.ts
- Add translation keys for: `auth.confirm_password`, `auth.passwords_mismatch`, `auth.forgot_password`, `auth.reset_password`, `auth.new_password`, `auth.password_reset_success`, `auth.enter_email_for_reset`.

---

## Verification Plan
- Register a new account → verify confirm password validation works (mismatch shows error, matching allows submit).
- On login page → click "Forgot Password?" → enter email → receive OTP → enter new password → login with new password.

## Guidance & Success Tasks Complete
- [x] Backend DTOs added
- [x] Backend Controllers added
- [x] Frontend methods implemented
- [x] Frontend HTML/TS updated for forgot/confirm password
