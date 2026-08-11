/* =====================================================================
   Glory2Him solution dependency data — consumed by index.html.

   Hand-maintained model of the solution's components and flows,
   generated from the actual source (2026-08-11). The 14 foundation
   services follow one template, so they are expanded from the entity
   configs below instead of being written out by hand.

   Shape:
     projects:   { id, name, kind: internal|library|external }
     components: { id, name, project, layer, col, methods[], utility?,
                   shared?, description? }
        - col: layout column (left → right)
        - utility: hidden unless the "utility brokers" toggle is on
        - shared: consumers link to ONE copy (library/external exposers)
          instead of getting a duplicated copy each
     events:     { id, publish, subscribe }  (row labels on EventBroker)
     edges:      direct    { kind:"direct", from:[comp,method|null],
                             to:[comp,method|null] }
                 publish   { kind:"publish", from:[comp,method], event }
                 subscribe { kind:"subscribe", event, to:[comp,handler] }
     roots:      component ids that start a tree (layout order)
   ===================================================================== */

(function () {
  const projects = [
    { id: "webapp", name: "Glory2Him.WebApp", kind: "internal" },
    { id: "core", name: "Glory2Him.Core", kind: "internal" },
    { id: "security-client", name: "G2H.Security.Client", kind: "library" },
    { id: "storage-client", name: "G2H.StorageClient", kind: "library" },
    { id: "ee-client", name: "G2H.EventEnvelope.Client", kind: "library" },
    { id: "ext-identity", name: "ASP.NET Core Identity", kind: "external" },
    { id: "ext-securitydb", name: "EF Core · SecurityDbContext", kind: "external" },
    { id: "ext-skiasharp", name: "SkiaSharp", kind: "external" },
    { id: "ext-eventhighway", name: "EventHighway", kind: "external" },
    { id: "ext-efcore", name: "EF Core / SQL Server", kind: "external" },
  ];

  const components = [];
  const events = [];
  const edges = [];
  const roots = [];

  const C = (comp) => { components.push(comp); return comp.id; };
  const D = (from, to) => edges.push({ kind: "direct", from, to });
  const P = (comp, method, event) => edges.push({ kind: "publish", from: [comp, method], event });
  const S = (event, comp, handler) => edges.push({ kind: "subscribe", event, to: [comp, handler] });

  /* ==================================================================
     Columns:
     0 WebApp exposers   1 WebApp view services   2 WebApp brokers
     3 WebApp externals  4 Core entry points      5 Core foundations
     6 Core brokers      7 client/external exposers
     8 client orchestrations  9 client foundations  10 client brokers
     11 far externals
     ================================================================== */

  /* ---------------- externals (shared, single copy) ---------------- */
  C({ id: "EXT.Identity", name: "ASP.NET Identity", project: "ext-identity", layer: "external", col: 3, shared: true, methods: [],
      description: "UserManager<AppUser> / SignInManager<AppUser> / IEmailSender<AppUser>. The WebApp talks to Identity directly from several endpoint groups and via IdentityBroker." });
  C({ id: "EXT.SecurityDb", name: "SecurityDbContext (EF Core)", project: "ext-securitydb", layer: "external", col: 3, shared: true, methods: [],
      description: "The WebApp's own EF Core Identity database (AppUser/AppRole). Not the Glory2Him.Core database." });
  C({ id: "EXT.SkiaSharp", name: "SkiaSharp", project: "ext-skiasharp", layer: "external", col: 3, shared: true, methods: [],
      description: "Image decode / crop / resize / WebP encode for profile avatars." });
  C({ id: "EXT.EventHighway", name: "EventHighwayClient", project: "ext-eventhighway", layer: "external", col: 7, shared: true,
      methods: [
        "V2.EventParticipantV2Client.RetrieveOrAddEventParticipantV2Async",
        "V2.EventAddressV2Client.RetrieveOrRegisterEventAddressV2Async",
        "V2.EventV2Client.SubmitEventV2Async",
        "V2.EventV2Client.FireScheduledPendingEventV2sAsync",
        "V2.RegisterEventHandlerAsync",
        "V2.EventListenerV2Client.RetrieveOrRegisterEventListenerV2Async",
      ],
      description: "EventHighway 2.2 (SQL Server storage). Every EventBroker copy publishes/subscribes through this client — one event address GUID per entity + operation; event name = entity + operation." });
  C({ id: "EXT.EFCore", name: "DbContext (EF Core / SQL)", project: "ext-efcore", layer: "external", col: 11, shared: true, methods: [],
      description: "The DbContext handed to EFCoreClient. In this solution that is Glory2Him.Core's StorageBroker itself (it derives from EFxceptionsContext and passes itself into EFCoreClient)." });

  /* ==================================================================
     Glory2Him.WebApp — standalone minimal-API host (React SPA).
     NOTE: the WebApp has NO reference to Glory2Him.Core today.
     ================================================================== */
  const waDesc = "Glory2Him.WebApp is currently standalone — it has no project reference to Glory2Him.Core; it serves the React SPA with its own Identity database and sample in-memory data.";

  C({ id: "WA.PostApi", name: "PostApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/posts", "GET /api/posts/slug/{slug}", "GET /api/posts/{id}", "POST /api/posts", "PUT /api/posts/{id}", "DELETE /api/posts/{id}"],
      description: "Minimal-API endpoint group. " + waDesc });
  C({ id: "WA.PostsView", name: "PostsViewService", project: "webapp", layer: "view", col: 1,
      methods: ["RetrieveAllPostsAsync", "RetrievePostBySlugAsync", "RetrievePostByIdAsync", "AddPostAsync", "ModifyPostAsync", "RemovePostAsync"],
      description: "Backed by the in-memory SamplePosts store (no database yet)." });
  C({ id: "WA.SamplePosts", name: "SamplePosts (in-memory)", project: "webapp", layer: "broker", col: 2, methods: ["All", "NextId"],
      description: "Static sample data store standing in for a future data source." });
  D(["WA.PostApi", "GET /api/posts"], ["WA.PostsView", "RetrieveAllPostsAsync"]);
  D(["WA.PostApi", "GET /api/posts/slug/{slug}"], ["WA.PostsView", "RetrievePostBySlugAsync"]);
  D(["WA.PostApi", "GET /api/posts/{id}"], ["WA.PostsView", "RetrievePostByIdAsync"]);
  D(["WA.PostApi", "POST /api/posts"], ["WA.PostsView", "AddPostAsync"]);
  D(["WA.PostApi", "PUT /api/posts/{id}"], ["WA.PostsView", "ModifyPostAsync"]);
  D(["WA.PostApi", "DELETE /api/posts/{id}"], ["WA.PostsView", "RemovePostAsync"]);
  for (const m of ["RetrieveAllPostsAsync", "RetrievePostBySlugAsync", "RetrievePostByIdAsync", "ModifyPostAsync", "RemovePostAsync"]) D(["WA.PostsView", m], ["WA.SamplePosts", "All"]);
  D(["WA.PostsView", "AddPostAsync"], ["WA.SamplePosts", "NextId"]);
  D(["WA.PostsView", "AddPostAsync"], ["WA.SamplePosts", "All"]);

  C({ id: "WA.ProductApi", name: "ProductApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/products", "GET /api/products/slug/{slug}"] });
  C({ id: "WA.ProductsView", name: "ProductsViewService", project: "webapp", layer: "view", col: 1,
      methods: ["RetrieveAllProductsAsync", "RetrieveProductBySlugAsync"],
      description: "Backed by the in-memory SampleProducts store." });
  C({ id: "WA.SampleProducts", name: "SampleProducts (in-memory)", project: "webapp", layer: "broker", col: 2, methods: ["All"] });
  D(["WA.ProductApi", "GET /api/products"], ["WA.ProductsView", "RetrieveAllProductsAsync"]);
  D(["WA.ProductApi", "GET /api/products/slug/{slug}"], ["WA.ProductsView", "RetrieveProductBySlugAsync"]);
  D(["WA.ProductsView", "RetrieveAllProductsAsync"], ["WA.SampleProducts", "All"]);
  D(["WA.ProductsView", "RetrieveProductBySlugAsync"], ["WA.SampleProducts", "All"]);

  C({ id: "WA.ProfileApi", name: "ProfileApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/profile", "PUT /api/profile", "POST /api/profile/image", "DELETE /api/profile/image", "GET /Profile-Image/{userId} (Program.cs)"] });
  C({ id: "WA.ProfileView", name: "ProfileViewService", project: "webapp", layer: "view", col: 1,
      methods: ["RetrieveProfileByIdAsync", "SetProfileImageAsync", "RemoveProfileImageAsync", "RetrieveProfileImageAsync"] });
  C({ id: "WA.ProfileImageBroker", name: "ProfileImageBroker", project: "webapp", layer: "broker", col: 2,
      methods: ["SelectUserByIdAsync", "UpdateProfileImageAsync"],
      description: "Reads/writes AppUser.ProfileImage via IDbContextFactory<SecurityDbContext>." });
  C({ id: "WA.ImageProcessingBroker", name: "ImageProcessingBroker", project: "webapp", layer: "broker", col: 2,
      methods: ["CreateSquareAvatarAsync"] });
  D(["WA.ProfileApi", "GET /api/profile"], ["WA.ProfileView", "RetrieveProfileByIdAsync"]);
  D(["WA.ProfileApi", "GET /api/profile"], ["EXT.Identity", "UserManager.GetUserAsync"]);
  D(["WA.ProfileApi", "PUT /api/profile"], ["EXT.Identity", "UserManager.GetUserAsync"]);
  D(["WA.ProfileApi", "PUT /api/profile"], ["EXT.Identity", "UserManager.SetPhoneNumberAsync"]);
  D(["WA.ProfileApi", "PUT /api/profile"], ["EXT.Identity", "UserManager.UpdateAsync"]);
  D(["WA.ProfileApi", "PUT /api/profile"], ["EXT.Identity", "SignInManager.RefreshSignInAsync"]);
  D(["WA.ProfileApi", "POST /api/profile/image"], ["WA.ProfileView", "SetProfileImageAsync"]);
  D(["WA.ProfileApi", "DELETE /api/profile/image"], ["WA.ProfileView", "RemoveProfileImageAsync"]);
  D(["WA.ProfileApi", "GET /Profile-Image/{userId} (Program.cs)"], ["WA.ProfileView", "RetrieveProfileImageAsync"]);
  D(["WA.ProfileView", "RetrieveProfileByIdAsync"], ["WA.ProfileImageBroker", "SelectUserByIdAsync"]);
  D(["WA.ProfileView", "SetProfileImageAsync"], ["WA.ImageProcessingBroker", "CreateSquareAvatarAsync"]);
  D(["WA.ProfileView", "SetProfileImageAsync"], ["WA.ProfileImageBroker", "UpdateProfileImageAsync"]);
  D(["WA.ProfileView", "RemoveProfileImageAsync"], ["WA.ProfileImageBroker", "UpdateProfileImageAsync"]);
  D(["WA.ProfileView", "RetrieveProfileImageAsync"], ["WA.ProfileImageBroker", "SelectUserByIdAsync"]);
  D(["WA.ProfileImageBroker", "SelectUserByIdAsync"], ["EXT.SecurityDb", "Users (DbSet<AppUser>)"]);
  D(["WA.ProfileImageBroker", "UpdateProfileImageAsync"], ["EXT.SecurityDb", "Users (DbSet<AppUser>)"]);
  D(["WA.ProfileImageBroker", "UpdateProfileImageAsync"], ["EXT.SecurityDb", "SaveChangesAsync"]);
  D(["WA.ImageProcessingBroker", "CreateSquareAvatarAsync"], ["EXT.SkiaSharp", "SKBitmap.Decode"]);
  D(["WA.ImageProcessingBroker", "CreateSquareAvatarAsync"], ["EXT.SkiaSharp", "SKBitmap.Resize"]);
  D(["WA.ImageProcessingBroker", "CreateSquareAvatarAsync"], ["EXT.SkiaSharp", "SKImage.FromBitmap"]);
  D(["WA.ImageProcessingBroker", "CreateSquareAvatarAsync"], ["EXT.SkiaSharp", "SKImage.Encode (WebP)"]);

  C({ id: "WA.RegistrationApi", name: "RegistrationApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/registrations/username-available", "GET /api/registrations/email-in-use", "GET /api/registrations/username-suggestions", "POST /api/registrations"] });
  C({ id: "WA.RegistrationView", name: "RegistrationViewService", project: "webapp", layer: "view", col: 1,
      methods: ["MinimumUsernameLength", "IsUsernameAvailableAsync", "IsEmailInUseAsync", "SuggestUsernamesAsync"] });
  C({ id: "WA.AccountBroker", name: "AccountBroker", project: "webapp", layer: "broker", col: 2,
      methods: ["UsernameExistsAsync", "EmailExistsAsync"],
      description: "Queries normalized username/email columns via IDbContextFactory<SecurityDbContext>." });
  D(["WA.RegistrationApi", "GET /api/registrations/username-available"], ["WA.RegistrationView", "IsUsernameAvailableAsync"]);
  D(["WA.RegistrationApi", "GET /api/registrations/username-available"], ["WA.RegistrationView", "MinimumUsernameLength"]);
  D(["WA.RegistrationApi", "GET /api/registrations/email-in-use"], ["WA.RegistrationView", "IsEmailInUseAsync"]);
  D(["WA.RegistrationApi", "GET /api/registrations/username-suggestions"], ["WA.RegistrationView", "SuggestUsernamesAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["WA.RegistrationView", "IsUsernameAvailableAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["WA.RegistrationView", "IsEmailInUseAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["EXT.Identity", "UserManager.CreateAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["EXT.Identity", "UserManager.AddToRoleAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["EXT.Identity", "IUserStore.SetUserNameAsync"]);
  D(["WA.RegistrationApi", "POST /api/registrations"], ["EXT.Identity", "IUserEmailStore.SetEmailAsync"]);
  D(["WA.RegistrationView", "IsUsernameAvailableAsync"], ["WA.AccountBroker", "UsernameExistsAsync"]);
  D(["WA.RegistrationView", "IsEmailInUseAsync"], ["WA.AccountBroker", "EmailExistsAsync"]);
  D(["WA.RegistrationView", "SuggestUsernamesAsync"], ["WA.AccountBroker", "UsernameExistsAsync"]);
  D(["WA.AccountBroker", "UsernameExistsAsync"], ["EXT.SecurityDb", "Users (DbSet<AppUser>)"]);
  D(["WA.AccountBroker", "EmailExistsAsync"], ["EXT.SecurityDb", "Users (DbSet<AppUser>)"]);

  C({ id: "WA.UserAdminApi", name: "UserAdminApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/admin/users", "GET /api/admin/users/roles", "GET /api/admin/users/{id}", "PUT /api/admin/users/{id}", "POST …/roles", "POST …/confirm-email", "POST …/locked-out", "POST …/reset-failed-count", "POST …/two-factor", "POST …/disabled", "POST …/confirmation-link", "POST …/password-reset-link", "DELETE /api/admin/users/{id}"] });
  C({ id: "WA.UsersView", name: "UsersViewService", project: "webapp", layer: "view", col: 1,
      methods: ["RetrieveAllUsersAsync", "RetrieveUserByIdAsync", "RetrieveAllRoleNamesAsync", "ModifyUserAsync", "SetUserRoleAsync", "ConfirmUserEmailAsync", "SetUserLockedOutAsync", "ResetAccessFailedCountAsync", "SetTwoFactorEnabledAsync", "SetUserDisabledAsync", "GenerateEmailConfirmationTokenAsync", "GeneratePasswordResetTokenAsync", "DeleteUserAsync"] });
  C({ id: "WA.IdentityBroker", name: "IdentityBroker", project: "webapp", layer: "broker", col: 2,
      methods: ["SelectAllUsers", "SelectUserByIdAsync", "SelectUserRolesAsync", "SelectUsersInRoleAsync", "SelectAllRoles", "SelectIsLockedOutAsync", "UpdateUserAsync", "SetUserNameAsync", "SetEmailAsync", "SetPhoneNumberAsync", "InsertUserToRoleAsync", "DeleteUserFromRoleAsync", "DeleteUserAsync", "GenerateEmailConfirmationTokenAsync", "ConfirmEmailAsync", "GeneratePasswordResetTokenAsync", "SetLockoutEnabledAsync", "SetLockoutEndDateAsync", "ResetAccessFailedCountAsync", "SetTwoFactorEnabledAsync", "ResetAuthenticatorKeyAsync"],
      description: "Wraps ASP.NET Identity UserManager<AppUser> + RoleManager<AppRole>." });
  const uaRoutes = {
    "GET /api/admin/users": ["RetrieveAllUsersAsync"],
    "GET /api/admin/users/roles": ["RetrieveAllRoleNamesAsync"],
    "GET /api/admin/users/{id}": ["RetrieveUserByIdAsync"],
    "PUT /api/admin/users/{id}": ["ModifyUserAsync"],
    "POST …/roles": ["SetUserRoleAsync"],
    "POST …/confirm-email": ["ConfirmUserEmailAsync"],
    "POST …/locked-out": ["SetUserLockedOutAsync"],
    "POST …/reset-failed-count": ["ResetAccessFailedCountAsync"],
    "POST …/two-factor": ["SetTwoFactorEnabledAsync"],
    "POST …/disabled": ["SetUserDisabledAsync"],
    "POST …/confirmation-link": ["GenerateEmailConfirmationTokenAsync"],
    "POST …/password-reset-link": ["GeneratePasswordResetTokenAsync"],
    "DELETE /api/admin/users/{id}": ["DeleteUserAsync"],
  };
  for (const [route, calls] of Object.entries(uaRoutes)) for (const call of calls) D(["WA.UserAdminApi", route], ["WA.UsersView", call]);
  const uvCalls = {
    RetrieveAllUsersAsync: ["SelectAllUsers", "SelectUserRolesAsync"],
    RetrieveUserByIdAsync: ["SelectUserByIdAsync", "SelectUserRolesAsync", "SelectIsLockedOutAsync"],
    RetrieveAllRoleNamesAsync: ["SelectAllRoles"],
    ModifyUserAsync: ["SelectUserByIdAsync", "UpdateUserAsync", "SetUserNameAsync", "SetEmailAsync", "SetPhoneNumberAsync"],
    SetUserRoleAsync: ["SelectUserByIdAsync", "InsertUserToRoleAsync", "DeleteUserFromRoleAsync", "SelectUserRolesAsync", "SelectUsersInRoleAsync"],
    ConfirmUserEmailAsync: ["SelectUserByIdAsync", "GenerateEmailConfirmationTokenAsync", "ConfirmEmailAsync"],
    SetUserLockedOutAsync: ["SelectUserByIdAsync", "SelectUserRolesAsync", "SelectUsersInRoleAsync", "SetLockoutEnabledAsync", "SetLockoutEndDateAsync"],
    ResetAccessFailedCountAsync: ["SelectUserByIdAsync", "ResetAccessFailedCountAsync"],
    SetTwoFactorEnabledAsync: ["SelectUserByIdAsync", "SetTwoFactorEnabledAsync", "ResetAuthenticatorKeyAsync"],
    SetUserDisabledAsync: ["SelectUserByIdAsync", "SelectUserRolesAsync", "SelectUsersInRoleAsync", "UpdateUserAsync", "SetLockoutEnabledAsync", "SetLockoutEndDateAsync"],
    GenerateEmailConfirmationTokenAsync: ["SelectUserByIdAsync", "GenerateEmailConfirmationTokenAsync"],
    GeneratePasswordResetTokenAsync: ["SelectUserByIdAsync", "GeneratePasswordResetTokenAsync"],
    DeleteUserAsync: ["SelectUserByIdAsync", "SelectUserRolesAsync", "SelectUsersInRoleAsync", "DeleteUserAsync"],
  };
  for (const [m, calls] of Object.entries(uvCalls)) for (const call of calls) D(["WA.UsersView", m], ["WA.IdentityBroker", call]);
  // verified against IdentityBroker.cs — 1:1 pass-throughs onto UserManager/RoleManager
  const ibToIdentity = {
    SelectAllUsers: "UserManager.Users",
    SelectUserByIdAsync: "UserManager.FindByIdAsync",
    InsertUserAsync: "UserManager.CreateAsync",
    DeleteUserAsync: "UserManager.DeleteAsync",
    SelectUserRolesAsync: "UserManager.GetRolesAsync",
    InsertUserToRoleAsync: "UserManager.AddToRoleAsync",
    DeleteUserFromRoleAsync: "UserManager.RemoveFromRoleAsync",
    SelectUsersInRoleAsync: "UserManager.GetUsersInRoleAsync",
    SelectIsLockedOutAsync: "UserManager.IsLockedOutAsync",
    UpdateUserAsync: "UserManager.UpdateAsync",
    SetUserNameAsync: "UserManager.SetUserNameAsync",
    SetEmailAsync: "UserManager.SetEmailAsync",
    SetPhoneNumberAsync: "UserManager.SetPhoneNumberAsync",
    GenerateEmailConfirmationTokenAsync: "UserManager.GenerateEmailConfirmationTokenAsync",
    ConfirmEmailAsync: "UserManager.ConfirmEmailAsync",
    GeneratePasswordResetTokenAsync: "UserManager.GeneratePasswordResetTokenAsync",
    SetLockoutEnabledAsync: "UserManager.SetLockoutEnabledAsync",
    SetLockoutEndDateAsync: "UserManager.SetLockoutEndDateAsync",
    ResetAccessFailedCountAsync: "UserManager.ResetAccessFailedCountAsync",
    SetTwoFactorEnabledAsync: "UserManager.SetTwoFactorEnabledAsync",
    ResetAuthenticatorKeyAsync: "UserManager.ResetAuthenticatorKeyAsync",
    SelectAllRoles: "RoleManager.Roles",
  };
  for (const [m, call] of Object.entries(ibToIdentity)) D(["WA.IdentityBroker", m], ["EXT.Identity", call]);

  C({ id: "WA.AccountApi", name: "AccountApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/accounts/me", "POST /api/accounts/login", "POST /api/accounts/login-2fa", "POST /api/accounts/login-recovery-code", "POST /api/accounts/logout", "POST /api/accounts/change-password", "POST /api/accounts/forgot-password", "POST /api/accounts/reset-password"],
      description: "Talks to ASP.NET Identity directly — no view service." });
  for (const [route, calls] of Object.entries({
    "GET /api/accounts/me": ["UserManager.GetUserAsync", "UserManager.GetRolesAsync"],
    "POST /api/accounts/login": ["UserManager.FindByNameAsync", "UserManager.FindByEmailAsync", "SignInManager.PasswordSignInAsync"],
    "POST /api/accounts/login-2fa": ["SignInManager.GetTwoFactorAuthenticationUserAsync", "SignInManager.TwoFactorAuthenticatorSignInAsync"],
    "POST /api/accounts/login-recovery-code": ["SignInManager.TwoFactorRecoveryCodeSignInAsync"],
    "POST /api/accounts/logout": ["SignInManager.SignOutAsync"],
    "POST /api/accounts/change-password": ["UserManager.ChangePasswordAsync", "SignInManager.RefreshSignInAsync"],
    "POST /api/accounts/forgot-password": ["UserManager.GeneratePasswordResetTokenAsync", "IEmailSender.SendPasswordResetLinkAsync"],
    "POST /api/accounts/reset-password": ["UserManager.ResetPasswordAsync"],
  })) for (const call of calls) D(["WA.AccountApi", route], ["EXT.Identity", call]);

  C({ id: "WA.PasskeyApi", name: "PasskeyApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["POST /api/passkeys/creation-options", "POST /api/passkeys/request-options", "POST /api/passkeys/register", "POST /api/passkeys/login", "GET /api/passkeys", "PUT /api/passkeys/{credentialId}", "DELETE /api/passkeys/{credentialId}", "GET /api/accounts/external-providers", "GET /api/accounts/external-logins", "POST /api/accounts/external-logins/remove"],
      description: "WebAuthn/passkeys + external logins — ASP.NET Identity directly, no view service." });
  for (const [route, calls] of Object.entries({
    "POST /api/passkeys/creation-options": ["SignInManager.MakePasskeyCreationOptionsAsync"],
    "POST /api/passkeys/request-options": ["SignInManager.MakePasskeyRequestOptionsAsync"],
    "POST /api/passkeys/register": ["SignInManager.PerformPasskeyAttestationAsync", "UserManager.AddOrUpdatePasskeyAsync"],
    "POST /api/passkeys/login": ["UserManager.FindByPasskeyIdAsync", "SignInManager.PasskeySignInAsync"],
    "GET /api/passkeys": ["UserManager.GetPasskeysAsync"],
    "PUT /api/passkeys/{credentialId}": ["UserManager.GetPasskeyAsync", "UserManager.AddOrUpdatePasskeyAsync"],
    "DELETE /api/passkeys/{credentialId}": ["UserManager.RemovePasskeyAsync"],
    "GET /api/accounts/external-providers": ["SignInManager.GetExternalAuthenticationSchemesAsync"],
    "GET /api/accounts/external-logins": ["UserManager.GetLoginsAsync", "UserManager.HasPasswordAsync"],
    "POST /api/accounts/external-logins/remove": ["UserManager.RemoveLoginAsync", "SignInManager.RefreshSignInAsync"],
  })) for (const call of calls) D(["WA.PasskeyApi", route], ["EXT.Identity", call]);

  C({ id: "WA.ManageAccountApi", name: "ManageAccountApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/manage/email", "POST /api/manage/email/change", "POST /api/manage/email/send-verification", "GET /api/manage/two-factor", "GET /api/manage/two-factor/authenticator", "GET /api/manage/two-factor/qr-code", "POST /api/manage/two-factor/verify", "POST /api/manage/two-factor/disable", "POST …/generate-recovery-codes", "POST …/reset-authenticator", "POST …/forget-browser", "GET /api/manage/personal-data", "GET …/personal-data/download", "POST /api/manage/delete-personal-data", "POST …/resend-email-confirmation", "POST /api/accounts/confirm-email", "POST …/confirm-email-change", "GET …/register-confirmation"],
      description: "Email / 2FA / personal-data management — ASP.NET Identity directly (QR codes via QRCoder), no view service." });
  for (const [route, calls] of Object.entries({
    "GET /api/manage/email": ["UserManager.GetEmailAsync", "UserManager.IsEmailConfirmedAsync"],
    "POST /api/manage/email/change": ["UserManager.GenerateChangeEmailTokenAsync", "IEmailSender.SendConfirmationLinkAsync"],
    "POST /api/manage/email/send-verification": ["UserManager.GenerateEmailConfirmationTokenAsync", "IEmailSender.SendConfirmationLinkAsync"],
    "GET /api/manage/two-factor": ["UserManager.GetAuthenticatorKeyAsync", "UserManager.GetTwoFactorEnabledAsync", "UserManager.CountRecoveryCodesAsync"],
    "GET /api/manage/two-factor/authenticator": ["UserManager.ResetAuthenticatorKeyAsync"],
    "GET /api/manage/two-factor/qr-code": ["UserManager.GetAuthenticatorKeyAsync"],
    "POST /api/manage/two-factor/verify": ["UserManager.VerifyTwoFactorTokenAsync", "UserManager.SetTwoFactorEnabledAsync", "UserManager.GenerateNewTwoFactorRecoveryCodesAsync"],
    "POST /api/manage/two-factor/disable": ["UserManager.SetTwoFactorEnabledAsync"],
    "POST …/generate-recovery-codes": ["UserManager.GenerateNewTwoFactorRecoveryCodesAsync"],
    "POST …/reset-authenticator": ["UserManager.ResetAuthenticatorKeyAsync", "SignInManager.RefreshSignInAsync"],
    "POST …/forget-browser": ["SignInManager.ForgetTwoFactorClientAsync"],
    "GET /api/manage/personal-data": ["UserManager.HasPasswordAsync"],
    "GET …/personal-data/download": ["UserManager.GetLoginsAsync", "UserManager.GetAuthenticatorKeyAsync"],
    "POST /api/manage/delete-personal-data": ["UserManager.CheckPasswordAsync", "UserManager.DeleteAsync", "SignInManager.SignOutAsync"],
    "POST …/resend-email-confirmation": ["UserManager.GenerateEmailConfirmationTokenAsync", "IEmailSender.SendConfirmationLinkAsync"],
    "POST /api/accounts/confirm-email": ["UserManager.ConfirmEmailAsync"],
    "POST …/confirm-email-change": ["UserManager.ChangeEmailAsync", "UserManager.SetUserNameAsync"],
    "GET …/register-confirmation": ["UserManager.GenerateEmailConfirmationTokenAsync"],
  })) for (const call of calls) D(["WA.ManageAccountApi", route], ["EXT.Identity", call]);

  C({ id: "WA.FrontendConfigApi", name: "FrontendConfigurationApiEndpoints", project: "webapp", layer: "exposer", col: 0,
      methods: ["GET /api/frontend-configurations"],
      description: "Reads IConfiguration only (YouVersion app key) — no downstream components." });

  C({ id: "WA.CartService", name: "CartService", project: "webapp", layer: "view", col: 1,
      methods: ["Add", "UpdateQuantity", "Remove", "Clear"],
      description: "Legacy Blazor-era in-memory demo cart — registered but not referenced by any API endpoint." });

  C({ id: "WA.LoggingBroker", name: "LoggingBroker", project: "webapp", layer: "broker", col: 2, utility: true,
      methods: ["LogInformationAsync", "LogTraceAsync", "LogDebugAsync", "LogWarningAsync", "LogErrorAsync", "LogCriticalAsync"],
      description: "Used by the view services' exception-path TryCatch only; exception-path calls are not drawn as edges." });

  /* ==================================================================
     Glory2Him.Core — events + entity template.
     ================================================================== */

  // Core brokers (duplicated per consumer by the renderer)
  C({ id: "StorageBroker", name: "StorageBroker", project: "core", layer: "broker", col: 6, methods: [],
      description: "EF Core DbContext (derives from EFxceptionsContext) exposing per-entity Insert/Select/Update/Delete plus ProcessedEvents bookkeeping. It passes ITSELF into G2H.StorageClient's EFCoreClient for the actual persistence operations." });
  C({ id: "SecurityAuditBroker", name: "SecurityAuditBroker", project: "core", layer: "broker", col: 6,
      methods: ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync", "GetUserIdAsync"],
      description: "Wraps G2H.Security.Client's AuditClient (ISecurityClient.Audits); each method also has a SecurityContext overload used during event replay." });
  C({ id: "SecurityBroker", name: "SecurityBroker", project: "core", layer: "broker", col: 6,
      methods: ["GetCurrentUserAsync", "IsCurrentUserAuthenticatedAsync", "IsInRoleAsync", "UserHasClaimAsync", "GetCurrentSecurityContextAsync"],
      description: "Wraps G2H.Security.Client's UserClient (ISecurityClient.Users); resolves the ClaimsPrincipal from IHttpContextAccessor or a JWT access token. Not yet consumed by any Core service." });
  C({ id: "EventEnvelopeBroker", name: "EventEnvelopeBroker", project: "core", layer: "broker", col: 6,
      methods: ["CreateAsync", "CreateNextAsync"],
      description: "Wraps G2H.EventEnvelope.Client — mints sealed envelopes (identity, integrity hash, causation chain) for every operation and event." });
  C({ id: "EventBroker", name: "EventBroker", project: "core", layer: "broker", col: 6,
      methods: ["RegisterEventParticipantAsync", "RegisterEventAddressesAsync", "FireScheduledPendingEventsAsync"],
      description: "Singleton wrapper over EventHighway (SQL Server). One Publish<Entity>Async / SubscribeTo<Entity>EventAsync pair per entity — the operation (Adding…HardRemoved) selects the event address. Request addresses (…ing) are subscribed by services; fact addresses (…ed) currently have no subscribers, so no circular flows exist." });
  C({ id: "DateTimeBroker", name: "DateTimeBroker", project: "core", layer: "broker", col: 6, utility: true, methods: ["GetCurrentDateTimeOffsetAsync"] });
  C({ id: "IdentifierBroker", name: "IdentifierBroker", project: "core", layer: "broker", col: 6, utility: true, methods: ["GetIdentifierAsync"] });
  C({ id: "HashBroker", name: "HashBroker", project: "core", layer: "broker", col: 6, utility: true, methods: ["ComputeSha256HashAsync"] });
  C({ id: "LoggingBroker", name: "LoggingBroker", project: "core", layer: "broker", col: 6, utility: true,
      methods: ["LogInformationAsync", "LogWarningAsync", "LogErrorAsync", "LogCriticalAsync"],
      description: "Happy-path denial logging on reads is drawn; exception-path logging (TryCatch) is not." });

  D(["EventBroker", "RegisterEventParticipantAsync"], ["EXT.EventHighway", "V2.EventParticipantV2Client.RetrieveOrAddEventParticipantV2Async"]);
  D(["EventBroker", "RegisterEventAddressesAsync"], ["EXT.EventHighway", "V2.EventAddressV2Client.RetrieveOrRegisterEventAddressV2Async"]);
  D(["EventBroker", "FireScheduledPendingEventsAsync"], ["EXT.EventHighway", "V2.EventV2Client.FireScheduledPendingEventV2sAsync"]);
  // ProcessedEvents bookkeeping maps onto the generic client surface
  D(["StorageBroker", "InsertProcessedEventAsync"], ["STC.EFCoreClient", "InsertAsync"]);
  D(["StorageBroker", "SelectProcessedEventExistsAsync"], ["STC.EFCoreClient", "ExistsAsync"]);
  D(["EventEnvelopeBroker", "CreateAsync"], ["EEC.Client", "CreateAsync"]);
  D(["EventEnvelopeBroker", "CreateNextAsync"], ["EEC.Client", "CreateNextAsync"]);
  for (const m of ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync", "GetUserIdAsync"])
    D(["SecurityAuditBroker", m], ["SC.AuditClient", m]);
  D(["SecurityBroker", "GetCurrentUserAsync"], ["SC.UserClient", "GetUserAsync"]);
  D(["SecurityBroker", "IsCurrentUserAuthenticatedAsync"], ["SC.UserClient", "IsUserAuthenticatedAsync"]);
  D(["SecurityBroker", "IsInRoleAsync"], ["SC.UserClient", "IsUserInRoleAsync"]);
  D(["SecurityBroker", "UserHasClaimAsync"], ["SC.UserClient", "UserHasClaimAsync"]);
  D(["SecurityBroker", "GetCurrentSecurityContextAsync"], ["SC.UserClient", "GetUserAsync"]);

  /* ---- foundation service template ----
     Variants (read-path differences):
       A: reads check publish-date visibility + ownership (DateTime + GetUserId)
       B: reads check ownership only (GetUserId)
       C: admin-gated settings — reads are storage-only (+ denial logging)
       D: fully public reads — no envelope, no security context on reads   */
  const entities = [
    { e: "ContentItem", variant: "A", extraCheck: true },
    { e: "Association", variant: "A" },
    { e: "BibleReference", variant: "A" },
    { e: "Comment", variant: "A" },
    { e: "Link", variant: "A" },
    { e: "Reaction", variant: "A" },
    { e: "Tag", variant: "A" },
    { e: "Approval", variant: "B" },
    { e: "ApprovalComment", variant: "B" },
    { e: "ApprovalReview", variant: "B" },
    { e: "ApprovalSetting", variant: "C" },
    { e: "ApprovalSettingPublisherRole", variant: "C" },
    { e: "ApprovalSettingReviewerRole", variant: "C" },
    { e: "ContentItemSetting", variant: "D" },
  ];

  const REQUEST_OPS = ["Adding", "Modifying", "RemovingById", "HardRemovingById", "RetrievingById"];
  const FACT_OPS = ["Added", "Modified", "Removed", "HardRemoved"];

  function defineEntityEvents(entity) {
    for (const op of REQUEST_OPS.concat(FACT_OPS)) {
      events.push({
        id: entity + "." + op,
        publish: "Publish" + entity + "Async",
        subscribe: "SubscribeTo" + entity + "EventAsync",
      });
    }
    // each EventBroker copy's publish/subscribe rows ride on EventHighway
    D(["EventBroker", "Publish" + entity + "Async"], ["EXT.EventHighway", "V2.EventV2Client.SubmitEventV2Async"]);
    D(["EventBroker", "SubscribeTo" + entity + "EventAsync"], ["EXT.EventHighway", "V2.RegisterEventHandlerAsync"]);
    D(["EventBroker", "SubscribeTo" + entity + "EventAsync"], ["EXT.EventHighway", "V2.EventListenerV2Client.RetrieveOrRegisterEventListenerV2Async"]);
  }

  for (const cfg of entities) {
    const e = cfg.e, v = cfg.variant, plural = e + "s";
    const svc = "FS." + e;
    defineEntityEvents(e);

    const add = `Add${e}Async`, retAll = `RetrieveAll${plural}Async`, retById = `Retrieve${e}ByIdAsync`;
    const modify = `Modify${e}Async`, remove = `Remove${e}ByIdAsync`, hardRemove = `HardRemove${e}ByIdAsync`;
    const onAdd = `OnAdding${e}Async`, onModify = `OnModifying${e}Async`, onRemove = `OnRemoving${e}ByIdAsync`;
    const onHardRemove = `OnHardRemoving${e}ByIdAsync`, onRetrieve = `OnRetrieving${e}ByIdAsync`;

    const methods = [add, retAll, retById, modify, remove, hardRemove];
    if (cfg.extraCheck) methods.splice(1, 0, `Check${e}ContentExistsAsync`);
    methods.push(onAdd, onModify, onRemove, onHardRemove, onRetrieve);

    const variantNote = {
      A: "Reads apply publish-date visibility + ownership checks.",
      B: "Reads apply ownership checks (no publish-date rule).",
      C: "Admin-gated settings entity — reads are storage-only with denial logging.",
      D: "Fully public reads — read paths take no security context and mint no envelope.",
    }[v];
    C({ id: svc, name: e + "Service", project: "core", layer: "foundation", col: 5, methods,
        description: `Foundation CRUD for ${e}. ${variantNote} The On*Async substrate handlers are wired by EventSubscriptionRegistration to the entity's request events; after the ProcessedEvents dedupe check they delegate to the same Do*Async path as the public methods (dual ProcessedEvents record: inbound + outbound envelope).` });

    const st = (m) => D([svc, m[0]], ["StorageBroker", m[1]]);
    const sa = (from, to) => D([svc, from], ["SecurityAuditBroker", to]);
    const ee = (from, to) => D([svc, from], ["EventEnvelopeBroker", to]);
    const dt = (from) => D([svc, from], ["DateTimeBroker", "GetCurrentDateTimeOffsetAsync"]);
    const idb = (from) => D([svc, from], ["IdentifierBroker", "GetIdentifierAsync"]);
    const lb = (from, to) => D([svc, from], ["LoggingBroker", to]);

    // -- Add
    ee(add, "CreateAsync");
    sa(add, "ApplyAddAuditValuesAsync");
    sa(add, "GetUserIdAsync");
    dt(add);
    st([add, `Insert${e}Async`]);
    idb(add);
    st([add, "InsertProcessedEventAsync"]);
    ee(add, "CreateNextAsync");
    P(svc, add, e + ".Added");

    // -- CheckContentExists (ContentItem only)
    if (cfg.extraCheck) {
      ee(`Check${e}ContentExistsAsync`, "CreateAsync");
      st([`Check${e}ContentExistsAsync`, `SelectAll${plural}Async`]);
    }

    // -- RetrieveAll
    if (v !== "D") ee(retAll, "CreateAsync");
    st([retAll, `SelectAll${plural}Async`]);
    if (v === "A") { dt(retAll); sa(retAll, "GetUserIdAsync"); }
    if (v === "B") sa(retAll, "GetUserIdAsync");

    // -- RetrieveById
    if (v !== "D") ee(retById, "CreateAsync");
    st([retById, `Select${e}ByIdAsync`]);
    if (v === "A") { dt(retById); sa(retById, "GetUserIdAsync"); }
    if (v === "B") sa(retById, "GetUserIdAsync");
    lb(retById, "LogInformationAsync");
    if (v !== "D") lb(retById, "LogWarningAsync");

    // -- Modify
    ee(modify, "CreateAsync");
    sa(modify, "ApplyModifyAuditValuesAsync");
    sa(modify, "GetUserIdAsync");
    dt(modify);
    st([modify, `Select${e}ByIdAsync`]);
    sa(modify, "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync");
    st([modify, `Update${e}Async`]);
    idb(modify);
    st([modify, "InsertProcessedEventAsync"]);
    ee(modify, "CreateNextAsync");
    P(svc, modify, e + ".Modified");

    // -- Remove (soft delete via Update)
    ee(remove, "CreateAsync");
    st([remove, `Select${e}ByIdAsync`]);
    if (v !== "C") sa(remove, "GetUserIdAsync");
    sa(remove, "ApplyRemoveAuditValuesAsync");
    st([remove, `Update${e}Async`]);
    idb(remove);
    st([remove, "InsertProcessedEventAsync"]);
    ee(remove, "CreateNextAsync");
    P(svc, remove, e + ".Removed");

    // -- HardRemove
    ee(hardRemove, "CreateAsync");
    st([hardRemove, `Select${e}ByIdAsync`]);
    st([hardRemove, `Delete${e}Async`]);
    idb(hardRemove);
    st([hardRemove, "InsertProcessedEventAsync"]);
    ee(hardRemove, "CreateNextAsync");
    P(svc, hardRemove, e + ".HardRemoved");

    // -- StorageBroker's per-entity partial rides on the shared EFCoreClient
    D(["StorageBroker", `Insert${e}Async`], ["STC.EFCoreClient", "InsertAsync"]);
    D(["StorageBroker", `SelectAll${plural}Async`], ["STC.EFCoreClient", "SelectAllAsync"]);
    D(["StorageBroker", `Select${e}ByIdAsync`], ["STC.EFCoreClient", "SelectAsync"]);
    D(["StorageBroker", `Update${e}Async`], ["STC.EFCoreClient", "UpdateAsync"]);
    D(["StorageBroker", `Delete${e}Async`], ["STC.EFCoreClient", "DeleteAsync"]);

    // -- substrate handlers: dedupe check, then delegate to the Do path
    for (const h of [onAdd, onModify, onRemove, onHardRemove]) st([h, "SelectProcessedEventExistsAsync"]);

    // -- subscriptions (wired centrally by EventSubscriptionRegistration)
    S(e + ".Adding", svc, onAdd);
    S(e + ".Modifying", svc, onModify);
    S(e + ".RemovingById", svc, onRemove);
    S(e + ".HardRemovingById", svc, onHardRemove);
    S(e + ".RetrievingById", svc, onRetrieve);
  }

  /* ---- ContentItemOrchestrationService ---- */
  const CIO = "CIO";
  for (const op of ["Adding", "Modifying", "RemovingById", "RetrievingById", "Added", "Modified", "Removed"]) {
    events.push({
      id: "ContentItemOrchestration." + op,
      publish: "PublishContentItemOrchestrationAsync",
      subscribe: "SubscribeToContentItemOrchestrationEventAsync",
    });
  }
  D(["EventBroker", "PublishContentItemOrchestrationAsync"], ["EXT.EventHighway", "V2.EventV2Client.SubmitEventV2Async"]);
  D(["EventBroker", "SubscribeToContentItemOrchestrationEventAsync"], ["EXT.EventHighway", "V2.RegisterEventHandlerAsync"]);
  D(["EventBroker", "SubscribeToContentItemOrchestrationEventAsync"], ["EXT.EventHighway", "V2.EventListenerV2Client.RetrieveOrRegisterEventListenerV2Async"]);
  C({ id: CIO, name: "ContentItemOrchestrationService", project: "core", layer: "orchestration", col: 4,
      methods: ["AddContentItemAsync", "ModifyContentItemAsync", "RemoveContentItemByIdAsync", "RetrieveContentItemByIdAsync", "RetrieveAllContentItemsAsync", "RetrieveAllPublicContentItemsAsync", "RetrieveContentItemsByGroupIdAsync", "RetrieveLatestContentItemByGroupIdAsync", "RetrievePublishedContentItemByGroupIdAsync", "OnAddingContentItemAsync", "OnModifyingContentItemAsync", "OnRemovingContentItemByIdAsync", "OnRetrievingContentItemByIdAsync"],
      description: "Core's public write/read surface for content: dedupe-by-hash, immutable-version forking on modify, group-based reads. Publishes ContentItemOrchestration facts (Added/Modified/Removed) exactly once per operation. The On*Async handlers delegate to the same Do*Async path as the public methods. Has NO IStorageBroker — ProcessedEvents dedupe lives in the foundation services." });

  const cioToCis = {
    AddContentItemAsync: ["CheckContentItemContentExistsAsync", "AddContentItemAsync"],
    ModifyContentItemAsync: ["RetrieveContentItemByIdAsync", "CheckContentItemContentExistsAsync", "ModifyContentItemAsync", "AddContentItemAsync"],
    RemoveContentItemByIdAsync: ["RetrieveContentItemByIdAsync", "RemoveContentItemByIdAsync"],
    RetrieveContentItemByIdAsync: ["RetrieveContentItemByIdAsync"],
    RetrieveAllContentItemsAsync: ["RetrieveAllContentItemsAsync"],
    RetrieveAllPublicContentItemsAsync: ["RetrieveAllContentItemsAsync"],
    RetrieveContentItemsByGroupIdAsync: ["RetrieveAllContentItemsAsync"],
    RetrieveLatestContentItemByGroupIdAsync: ["RetrieveAllContentItemsAsync"],
    RetrievePublishedContentItemByGroupIdAsync: ["RetrieveAllContentItemsAsync"],
  };
  for (const [m, calls] of Object.entries(cioToCis)) for (const call of calls) D([CIO, m], ["FS.ContentItem", call]);

  for (const m of ["AddContentItemAsync", "ModifyContentItemAsync", "RemoveContentItemByIdAsync", "RetrieveContentItemByIdAsync", "RetrieveAllContentItemsAsync", "RetrieveContentItemsByGroupIdAsync", "RetrieveLatestContentItemByGroupIdAsync", "RetrievePublishedContentItemByGroupIdAsync"])
    D([CIO, m], ["EventEnvelopeBroker", "CreateAsync"]);
  for (const m of ["AddContentItemAsync", "ModifyContentItemAsync", "RemoveContentItemByIdAsync"])
    D([CIO, m], ["EventEnvelopeBroker", "CreateNextAsync"]);
  for (const m of ["AddContentItemAsync", "ModifyContentItemAsync"]) {
    D([CIO, m], ["HashBroker", "ComputeSha256HashAsync"]);
    D([CIO, m], ["IdentifierBroker", "GetIdentifierAsync"]);
  }
  for (const m of ["ModifyContentItemAsync", "RemoveContentItemByIdAsync", "RetrieveContentItemByIdAsync", "RetrieveAllContentItemsAsync", "RetrieveContentItemsByGroupIdAsync", "RetrieveLatestContentItemByGroupIdAsync", "RetrievePublishedContentItemByGroupIdAsync"])
    D([CIO, m], ["SecurityAuditBroker", "GetUserIdAsync"]);
  for (const m of ["RetrieveContentItemByIdAsync", "RetrieveAllContentItemsAsync", "RetrieveAllPublicContentItemsAsync", "RetrieveContentItemsByGroupIdAsync", "RetrieveLatestContentItemByGroupIdAsync", "RetrievePublishedContentItemByGroupIdAsync"])
    D([CIO, m], ["DateTimeBroker", "GetCurrentDateTimeOffsetAsync"]);
  for (const m of ["RetrieveContentItemByIdAsync", "RetrieveLatestContentItemByGroupIdAsync", "RetrievePublishedContentItemByGroupIdAsync"]) {
    D([CIO, m], ["LoggingBroker", "LogInformationAsync"]);
    D([CIO, m], ["LoggingBroker", "LogWarningAsync"]);
  }

  P(CIO, "AddContentItemAsync", "ContentItemOrchestration.Added");
  P(CIO, "ModifyContentItemAsync", "ContentItemOrchestration.Modified");
  P(CIO, "RemoveContentItemByIdAsync", "ContentItemOrchestration.Removed");
  S("ContentItemOrchestration.Adding", CIO, "OnAddingContentItemAsync");
  S("ContentItemOrchestration.Modifying", CIO, "OnModifyingContentItemAsync");
  S("ContentItemOrchestration.RemovingById", CIO, "OnRemovingContentItemByIdAsync");
  S("ContentItemOrchestration.RetrievingById", CIO, "OnRetrievingContentItemByIdAsync");

  /* ---- EventSubscriptionRegistration ---- */
  C({ id: "ESR", name: "EventSubscriptionRegistration", project: "core", layer: "registration", col: 4,
      methods: ["RegisterAsync"],
      description: "Core's public event entry point for hosts: registers the Glory2Him participant + all event addresses on EventHighway, then wires all 74 subscriptions (every purple line in this graph). Not yet called by any production host in this repo — only the unit tests." });
  D(["ESR", "RegisterAsync"], ["EventBroker", "RegisterEventParticipantAsync"]);
  D(["ESR", "RegisterAsync"], ["EventBroker", "RegisterEventAddressesAsync"]);

  /* ==================================================================
     G2H.Security.Client (library — internals shown once)
     ================================================================== */
  C({ id: "SC.UserClient", name: "UserClient (ISecurityClient.Users)", project: "security-client", layer: "exposer", col: 7, shared: true,
      methods: ["GetUserAsync", "GetUserIdAsync", "IsUserAuthenticatedAsync", "IsUserInRoleAsync", "UserHasClaimAsync", "GetUserClaimValueAsync", "GetUserClaimValuesAsync"],
      description: "Public user/claims surface of G2H.Security.Client. Shown once — every consumer links to this copy." });
  C({ id: "SC.AuditClient", name: "AuditClient (ISecurityClient.Audits)", project: "security-client", layer: "exposer", col: 7, shared: true,
      methods: ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync", "EnsureOtherAuditValuesRemainsUnchangedOnRemoveAsync", "GetUserIdAsync"],
      description: "Public audit-stamping surface of G2H.Security.Client." });
  C({ id: "SC.AuditOrchestration", name: "AuditOrchestrationService", project: "security-client", layer: "orchestration", col: 8,
      methods: ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync", "GetCurrentUserIdAsync"] });
  C({ id: "SC.UserService", name: "UserService", project: "security-client", layer: "foundation", col: 9,
      methods: ["GetUserAsync", "GetUserIdAsync", "IsUserAuthenticatedAsync", "IsUserInRoleAsync", "UserHasClaimAsync", "GetUserClaimValueAsync", "GetUserClaimValuesAsync"] });
  C({ id: "SC.AuditService", name: "AuditService", project: "security-client", layer: "foundation", col: 9,
      methods: ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync"] });
  C({ id: "SC.DateTimeBroker", name: "DateTimeBroker", project: "security-client", layer: "broker", col: 10, utility: true,
      methods: ["GetCurrentDateTimeOffsetAsync"] });

  for (const m of ["GetUserAsync", "GetUserIdAsync", "IsUserAuthenticatedAsync", "IsUserInRoleAsync", "UserHasClaimAsync", "GetUserClaimValueAsync", "GetUserClaimValuesAsync"])
    D(["SC.UserClient", m], ["SC.UserService", m]);
  for (const m of ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync"])
    D(["SC.AuditClient", m], ["SC.AuditOrchestration", m]);
  D(["SC.AuditClient", "GetUserIdAsync"], ["SC.AuditOrchestration", "GetCurrentUserIdAsync"]);
  for (const m of ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync"]) {
    D(["SC.AuditOrchestration", m], ["SC.UserService", "GetUserIdAsync"]);
    D(["SC.AuditOrchestration", m], ["SC.AuditService", m]);
  }
  D(["SC.AuditOrchestration", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync"], ["SC.AuditService", "EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync"]);
  D(["SC.AuditOrchestration", "GetCurrentUserIdAsync"], ["SC.UserService", "GetUserIdAsync"]);
  for (const m of ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync"])
    D(["SC.AuditService", m], ["SC.DateTimeBroker", "GetCurrentDateTimeOffsetAsync"]);

  /* ==================================================================
     G2H.StorageClient (library)
     ================================================================== */
  C({ id: "STC.EFCoreClient", name: "EFCoreClient (IEFCoreClient)", project: "storage-client", layer: "exposer", col: 7, shared: true,
      methods: ["InsertAsync", "SelectAllAsync", "SelectAsync", "UpdateAsync", "DeleteAsync", "BulkInsertAsync", "BulkReadAsync", "BulkUpdateAsync", "BulkDeleteAsync", "BulkUpsertAsync", "ExistsAsync"],
      description: "Generic relational persistence client. Glory2Him.Core's StorageBroker constructs it with itself as the DbContext." });
  C({ id: "STC.OperationService", name: "OperationService", project: "storage-client", layer: "foundation", col: 9,
      methods: ["InsertAsync", "SelectAllAsync", "SelectAsync", "UpdateAsync", "DeleteAsync", "BulkInsertAsync", "BulkReadAsync", "BulkUpdateAsync", "BulkDeleteAsync", "BulkUpsertAsync", "ExistsAsync"] });
  C({ id: "STC.StorageBroker", name: "StorageBroker (client)", project: "storage-client", layer: "broker", col: 10, shared: true,
      methods: ["FindEntityTypeAsync", "SelectAllAsync", "SelectAsync", "UpdateObjectStateAsync", "SaveChangesAsync", "BeginTransactionAsync", "BulkInsertAsync", "BulkUpdateAsync", "BulkDeleteAsync"],
      description: "G2H.StorageClient's internal broker over the supplied DbContext (change tracking, transactions, bulk operations)." });
  for (const m of ["InsertAsync", "SelectAllAsync", "SelectAsync", "UpdateAsync", "DeleteAsync", "BulkInsertAsync", "BulkReadAsync", "BulkUpdateAsync", "BulkDeleteAsync", "BulkUpsertAsync", "ExistsAsync"])
    D(["STC.EFCoreClient", m], ["STC.OperationService", m]);
  // verified against OperationService.cs — each operation drives the broker's
  // change-tracking / transaction surface
  const opsToBroker = {
    InsertAsync: ["UpdateObjectStateAsync", "SaveChangesAsync"],
    SelectAllAsync: ["SelectAllAsync"],
    SelectAsync: ["SelectAsync"],
    UpdateAsync: ["UpdateObjectStateAsync", "SaveChangesAsync"],
    DeleteAsync: ["UpdateObjectStateAsync", "SaveChangesAsync"],
    BulkInsertAsync: ["BeginTransactionAsync", "BulkInsertAsync", "SaveChangesAsync", "UpdateObjectStateAsync"],
    BulkReadAsync: ["FindEntityTypeAsync", "SelectAllAsync"],
    BulkUpdateAsync: ["BeginTransactionAsync", "BulkUpdateAsync", "SaveChangesAsync", "UpdateObjectStateAsync"],
    BulkDeleteAsync: ["BeginTransactionAsync", "BulkDeleteAsync", "SaveChangesAsync", "UpdateObjectStateAsync"],
    BulkUpsertAsync: ["FindEntityTypeAsync", "BeginTransactionAsync", "SelectAllAsync", "BulkInsertAsync", "BulkUpdateAsync", "SaveChangesAsync", "UpdateObjectStateAsync"],
    ExistsAsync: ["FindEntityTypeAsync", "SelectAllAsync"],
  };
  for (const [m, calls] of Object.entries(opsToBroker)) for (const call of calls) D(["STC.OperationService", m], ["STC.StorageBroker", call]);
  // verified against G2H.StorageClient StorageBroker.cs — the raw DbContext surface
  const scbToEfCore = {
    FindEntityTypeAsync: ["Model.FindEntityType"],
    SaveChangesAsync: ["SaveChangesAsync"],
    BeginTransactionAsync: ["Database.BeginTransactionAsync"],
    SelectAllAsync: ["Set<T>()"],
    SelectAsync: ["FindAsync"],
    UpdateObjectStateAsync: ["Entry(object).State"],
    BulkInsertAsync: ["AddRangeAsync"],
    BulkUpdateAsync: ["UpdateRange"],
    BulkDeleteAsync: ["RemoveRange"],
  };
  for (const [m, calls] of Object.entries(scbToEfCore)) for (const call of calls) D(["STC.StorageBroker", m], ["EXT.EFCore", call]);

  /* ==================================================================
     G2H.EventEnvelope.Client (library)
     ================================================================== */
  C({ id: "EEC.Client", name: "EventEnvelopeClient", project: "ee-client", layer: "exposer", col: 7, shared: true,
      methods: ["CreateAsync", "CreateNextAsync"],
      description: "Creates sealed, integrity-hashed event envelopes (identity, security context, causation chain). Validation and sealing happen inside Create/CreateNext." });
  C({ id: "EEC.Service", name: "EventEnvelopeService", project: "ee-client", layer: "foundation", col: 9,
      methods: ["CreateAsync", "CreateNextAsync"] });
  C({ id: "EEC.SecurityBroker", name: "SecurityBroker", project: "ee-client", layer: "broker", col: 10,
      methods: ["GetCurrentSecurityContextAsync", "GetUserAsync", "IsUserAuthenticatedAsync", "IsUserInRoleAsync", "UserHasClaimAsync"],
      description: "The envelope client's own security broker — wraps G2H.Security.Client's UserClient. CreateAsync uses it to resolve the security context stamped into each envelope." });
  C({ id: "EEC.SecurityAuditBroker", name: "SecurityAuditBroker", project: "ee-client", layer: "broker", col: 10,
      methods: ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "GetUserIdAsync"],
      description: "Wraps G2H.Security.Client's AuditClient. Registered in the envelope client's DI but not yet called by EventEnvelopeService." });
  C({ id: "EEC.DateTimeBroker", name: "DateTimeBroker", project: "ee-client", layer: "broker", col: 10, utility: true, methods: ["GetCurrentDateTimeOffsetAsync"] });
  C({ id: "EEC.IdentifierBroker", name: "IdentifierBroker", project: "ee-client", layer: "broker", col: 10, utility: true, methods: ["GetIdentifierAsync"] });
  C({ id: "EEC.LoggingBroker", name: "LoggingBroker", project: "ee-client", layer: "broker", col: 10, utility: true, methods: ["LogErrorAsync", "LogCriticalAsync"] });

  D(["EEC.Client", "CreateAsync"], ["EEC.Service", "CreateAsync"]);
  D(["EEC.Client", "CreateNextAsync"], ["EEC.Service", "CreateNextAsync"]);
  // verified against EventEnvelopeService.cs: CreateAsync resolves security
  // context + ids + timestamp; CreateNextAsync only mints the next event id
  D(["EEC.Service", "CreateAsync"], ["EEC.SecurityBroker", "GetCurrentSecurityContextAsync"]);
  D(["EEC.Service", "CreateAsync"], ["EEC.IdentifierBroker", "GetIdentifierAsync"]);
  D(["EEC.Service", "CreateAsync"], ["EEC.DateTimeBroker", "GetCurrentDateTimeOffsetAsync"]);
  D(["EEC.Service", "CreateNextAsync"], ["EEC.IdentifierBroker", "GetIdentifierAsync"]);
  D(["EEC.SecurityBroker", "GetCurrentSecurityContextAsync"], ["SC.UserClient", "GetUserAsync"]);
  for (const m of ["GetUserAsync", "IsUserAuthenticatedAsync", "IsUserInRoleAsync", "UserHasClaimAsync"])
    D(["EEC.SecurityBroker", m], ["SC.UserClient", m]);
  for (const m of ["ApplyAddAuditValuesAsync", "ApplyModifyAuditValuesAsync", "ApplyRemoveAuditValuesAsync", "GetUserIdAsync"])
    D(["EEC.SecurityAuditBroker", m], ["SC.AuditClient", m]);

  /* ==================================================================
     roots — tree order controls the vertical layout
     ================================================================== */
  roots.push(
    // Glory2Him.WebApp
    "WA.PostApi", "WA.ProductApi", "WA.ProfileApi", "WA.RegistrationApi",
    "WA.UserAdminApi", "WA.AccountApi", "WA.PasskeyApi", "WA.ManageAccountApi",
    "WA.FrontendConfigApi", "WA.CartService",
    // Glory2Him.Core
    "CIO", "ESR", "SecurityBroker",
    ...entities.map(c => "FS." + c.e),
    // client libraries (single shared copies + their internals)
    "SC.UserClient", "SC.AuditClient",
    "STC.EFCoreClient", "STC.StorageBroker",
    "EEC.Client", "EEC.SecurityAuditBroker",
    // externals
    "EXT.EventHighway", "EXT.Identity", "EXT.SecurityDb", "EXT.SkiaSharp", "EXT.EFCore",
  );

  /* ------------------------------------------------------------------
     Externals show exactly the public surface this solution calls.
     Derive their method rows from the declared edges so the rows and
     the arrows can never drift apart.
     ------------------------------------------------------------------ */
  for (const extId of ["EXT.Identity", "EXT.SecurityDb", "EXT.SkiaSharp", "EXT.EFCore"]) {
    const comp = components.find(c => c.id === extId);
    const called = [];
    for (const e of edges) {
      if (e.kind === "direct" && e.to[0] === extId && e.to[1] && !called.includes(e.to[1])) called.push(e.to[1]);
    }
    comp.methods = called.sort((a, b) => a.localeCompare(b));
  }

  window.G2H_DATA = {
    projects,
    components,
    events,
    edges,
    roots,
    eventBrokerId: "EventBroker",
  };
})();
