# G2H.Security.Client

A [Standard](https://github.com/hassanhabib/The-Standard) compliant client that abstracts security operations away from infrastructure brokers, keeping your foundation services clean and testable.

The library exposes two focused clients through a single `ISecurityClient` entry point:

| Client | Purpose |
|---|---|
| `IUserClient` | Extract user identity, roles, and claims from a `ClaimsPrincipal` |
| `IAuditClient` | Apply and protect audit metadata (`CreatedBy`, `CreatedWhen`, `UpdatedBy`, `UpdatedWhen`, `DeletedBy`, `DeletedWhen`) on any entity |

Because all security concerns are resolved through the `ISecurityAuditBroker` abstraction (rather than being called directly inside a service), every service method remains fully unit-testable without standing up a real HTTP context or JWT infrastructure.

---

## Table of Contents

- [Installation and Registration](#installation-and-registration)
- [SecurityConfigurations](#securityconfigurations)
- [The Security Broker](#the-security-broker)
- [Audit Client](#audit-client)
  - [ApplyAddAuditValuesAsync](#applyaditvaluesasync)
  - [ApplyModifyAuditValuesAsync](#applymodifyauditvaluesasync)
  - [ApplyRemoveAuditValuesAsync](#applyremoveauditvaluesasync)
  - [EnsureAddAuditValuesRemainsUnchangedOnModifyAsync](#ensureadauditvaluesremainsunchangedonmodifyasync)
- [User Client](#user-client)
  - [GetUserAsync](#getuserasync)
  - [GetUserIdAsync](#getuseridwasync)
  - [IsUserAuthenticatedAsync](#isuserauthenticatedasync)
  - [IsUserInRoleAsync](#isuserinroleasync)
  - [UserHasClaimAsync](#userhasclamasync)
  - [GetUserClaimValueAsync](#getuserclamvalueasync)
  - [GetUserClaimValuesAsync](#getuserclamvaluesasync)
- [Testing with Mocks](#testing-with-mocks)

---

## Installation and Registration

Register the broker in your DI container. For a REST API host (where the current user is available via `IHttpContextAccessor`):

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISecurityAuditBroker>(provider =>
    new SecurityAuditBroker(
        httpContextAccessor: provider.GetRequiredService<IHttpContextAccessor>(),
        securityConfigurations: new SecurityConfigurations()));
```

For Azure Functions or any non-HTTP host where you supply the token yourself:

```csharp
// Function host startup
services.AddScoped<ISecurityAuditBroker>(provider =>
    new SecurityAuditBroker(
        accessToken: Environment.GetEnvironmentVariable("ACCESS_TOKEN"),
        securityConfigurations: new SecurityConfigurations()));
```

---

## SecurityConfigurations

`SecurityConfigurations` tells the audit engine which property names and types to target on your entities. The defaults match the most common naming convention and rarely need to change.

```csharp
var configurations = new SecurityConfigurations
{
    CreatedByPropertyName   = "CreatedBy",
    CreatedByPropertyType   = typeof(string),
    CreatedDatePropertyName = "CreatedWhen",
    CreatedDatePropertyType = typeof(DateTimeOffset),

    UpdatedByPropertyName   = "UpdatedBy",
    UpdatedByPropertyType   = typeof(string),
    UpdatedDatePropertyName = "UpdatedWhen",
    UpdatedDatePropertyType = typeof(DateTimeOffset),

    DeletedByPropertyName   = "DeletedBy",
    DeletedByPropertyType   = typeof(string),
    DeletedDatePropertyName = "DeletedWhen",
    DeletedDatePropertyType = typeof(DateTimeOffset),
};
```

If your entity uses different names (e.g. `ModifiedOn` instead of `UpdatedWhen`) simply change the corresponding property name — the client will find and set the right field via reflection.

---

## The Security Broker

The `ISecurityAuditBroker` is the thin abstraction that your foundation services depend on. It resolves the current user from the ambient context (HTTP or token) so that your service methods never need to know about `ClaimsPrincipal`, `IHttpContextAccessor`, or JWT parsing.

```csharp
public interface ISecurityAuditBroker
{
    ValueTask<T>      ApplyAddAuditValuesAsync<T>(T entity);
    ValueTask<T>      ApplyModifyAuditValuesAsync<T>(T entity);
    ValueTask<T>      ApplyRemoveAuditValuesAsync<T>(T entity);
    ValueTask<T>      EnsureAddAuditValuesRemainsUnchangedOnModifyAsync<T>(T entity, T storageEntity);
    ValueTask<string> GetUserIdAsync();
}
```

Inject it into your foundation service the same way as any other broker:

```csharp
public class StudentService : IStudentService
{
    private readonly IStorageBroker storageBroker;
    private readonly IDateTimeBroker dateTimeBroker;
    private readonly ISecurityAuditBroker securityAuditBroker;
    private readonly ILoggingBroker loggingBroker;

    public StudentService(
        IStorageBroker storageBroker,
        IDateTimeBroker dateTimeBroker,
        ISecurityAuditBroker securityAuditBroker,
        ILoggingBroker loggingBroker)
    {
        this.storageBroker = storageBroker;
        this.dateTimeBroker = dateTimeBroker;
        this.securityAuditBroker = securityAuditBroker;
        this.loggingBroker = loggingBroker;
    }
}
```

---

## Audit Client

### ApplyAddAuditValuesAsync

Called **before** inserting a new entity. Sets `CreatedBy`, `CreatedWhen`, `UpdatedBy`, and `UpdatedWhen` all to the current user and current UTC timestamp.

```csharp
public ValueTask<Student> AddStudentAsync(
    Student student,
    CancellationToken cancellationToken = default) =>
    TryCatch(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Audit values are applied first, then validation runs against them.
        // This means your validation rules (e.g. CreatedBy is required,
        // CreatedDate must be recent) will pass on the audited object.
        student = await this.securityAuditBroker.ApplyAddAuditValuesAsync(student);
        await ValidateOnAddStudent(student);

        Student addedStudent =
            await this.storageBroker.InsertStudentAsync(student, cancellationToken);

        var envelope = new EventEnvelope<Student> { Content = addedStudent };
        await this.eventBroker.PublishStudentAsync(envelope, "StudentAdded");

        return addedStudent;
    });
```

After this call a `Student` object will look like:

```
student.CreatedBy   = "alice@school.edu"
student.CreatedWhen = 2025-09-05T17:00:00Z
student.UpdatedBy   = "alice@school.edu"
student.UpdatedWhen = 2025-09-05T17:00:00Z
```

---

### ApplyModifyAuditValuesAsync

Called **before** updating an existing entity. Sets only `UpdatedBy` and `UpdatedWhen`, leaving the creation fields untouched.

```csharp
public ValueTask<Student> ModifyStudentAsync(
    Student student,
    CancellationToken cancellationToken = default) =>
    TryCatch(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        student = await this.securityAuditBroker.ApplyModifyAuditValuesAsync(student);
        await ValidateOnModifyStudent(student);

        Student maybeStudent =
            await this.storageBroker.SelectStudentByIdAsync(student.Id, cancellationToken);

        ValidateStorageStudent(maybeStudent, student.Id);

        // Protects CreatedBy / CreatedDate from being overwritten.
        student = await this.securityAuditBroker
            .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(student, maybeStudent);

        ValidateAgainstStorageStudentOnModify(
            inputStudent: student,
            storageStudent: maybeStudent);

        Student updatedStudent =
            await this.storageBroker.UpdateStudentAsync(student, cancellationToken);

        var envelope = new EventEnvelope<Student> { Content = updatedStudent };
        await this.eventBroker.PublishStudentAsync(envelope, "StudentModified");

        return updatedStudent;
    });
```

After this call:

```
student.UpdatedBy   = "bob@school.edu"
student.UpdatedWhen = 2025-09-05T18:30:00Z
// CreatedBy and CreatedWhen are unchanged
```

---

### ApplyRemoveAuditValuesAsync

Called during a **soft-delete** operation before setting the deleted flag. Sets `DeletedBy` and `DeletedWhen`.

```csharp
public ValueTask<Student> RemoveStudentByIdAsync(
    Guid studentId,
    CancellationToken cancellationToken = default) =>
    TryCatch(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateOnRemoveStudentById(studentId);

        Student maybeStudent =
            await this.storageBroker.SelectStudentByIdAsync(studentId, cancellationToken);

        ValidateStorageStudent(maybeStudent, studentId);

        // Idempotent: if already soft-deleted, return as-is.
        if (maybeStudent.IsDeleted)
            return maybeStudent;

        maybeStudent = await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(maybeStudent);
        maybeStudent.IsDeleted = true;

        Student deletedStudent =
            await this.storageBroker.UpdateStudentAsync(maybeStudent, cancellationToken);

        var envelope = new EventEnvelope<Student> { Content = deletedStudent };
        await this.eventBroker.PublishStudentAsync(envelope, "StudentRemoved");

        return deletedStudent;
    });
```

After this call:

```
student.DeletedBy   = "charlie@school.edu"
student.DeletedWhen = 2025-09-05T19:00:00Z
student.IsDeleted   = true
```

---

### EnsureAddAuditValuesRemainsUnchangedOnModifyAsync

Guards against callers (or attackers) attempting to rewrite immutable creation audit fields. It copies `CreatedBy` and `CreatedWhen` back from the stored entity onto the incoming entity, ensuring those values can never be changed via a modify operation.

```csharp
// Inside ModifyStudentAsync, after selecting from storage:
student = await this.securityAuditBroker
    .EnsureAddAuditValuesRemainsUnchangedOnModifyAsync(student, maybeStudent);
```

If a caller submits `CreatedBy = "mallory"`, this method silently restores `CreatedBy = "alice@school.edu"` from storage before validation or persistence runs.

---

## User Client

The `IUserClient` is available via `ISecurityClient.Users` and operates directly on a `ClaimsPrincipal`. It is most useful in controller-layer or middleware code that needs to inspect the caller before delegating to a service.

### GetUserAsync

Returns a fully populated `User` object from the claims principal.

```csharp
User currentUser = await securityClient.Users.GetUserAsync(claimsPrincipal);

Console.WriteLine(currentUser.UserId);      // "12345"
Console.WriteLine(currentUser.DisplayName); // "Alice Smith"
Console.WriteLine(currentUser.Email);       // "alice@school.edu"
Console.WriteLine(currentUser.JobTitle);    // "Teacher"
```

The `User` model exposes:

| Property | Type | Description |
|---|---|---|
| `UserId` | `string` | Unique identifier extracted from claims |
| `GivenName` | `string` | First name |
| `Surname` | `string` | Last name |
| `DisplayName` | `string` | Full display name |
| `Email` | `string` | Email address |
| `JobTitle` | `string` | Job title claim |
| `Roles` | `IEnumerable<string>` | All role claims |
| `Claims` | `IEnumerable<Claim>` | All raw claims |

---

### GetUserIdAsync

Returns just the user identifier — the most common need in audit scenarios.

```csharp
string userId = await securityClient.Users.GetUserIdAsync(claimsPrincipal);
// "alice@school.edu" or "Anonymous" if unauthenticated
```

---

### IsUserAuthenticatedAsync

```csharp
bool isAuthenticated = await securityClient.Users.IsUserAuthenticatedAsync(claimsPrincipal);

if (!isAuthenticated)
    throw new UnauthorizedAccessException("You must be signed in.");
```

---

### IsUserInRoleAsync

```csharp
bool isAdmin = await securityClient.Users.IsUserInRoleAsync(claimsPrincipal, "Admin");

if (!isAdmin)
    throw new ForbiddenAccessException("Only admins can access this resource.");
```

---

### UserHasClaimAsync

Check for a specific claim type and value:

```csharp
bool canGrade = await securityClient.Users.UserHasClaimAsync(
    claimsPrincipal,
    type: "Permission",
    value: "Grade.Write");
```

Check that at least one claim of a type exists:

```csharp
bool hasAnyPermission = await securityClient.Users.UserHasClaimAsync(
    claimsPrincipal,
    type: "Permission");
```

---

### GetUserClaimValueAsync

Returns the first value for a given claim type:

```csharp
string department = await securityClient.Users.GetUserClaimValueAsync(
    claimsPrincipal,
    type: "Department");
// e.g. "Mathematics"
```

---

### GetUserClaimValuesAsync

Returns all values for a given claim type — useful for multi-value claims such as roles or permissions:

```csharp
IReadOnlyList<string> roles = await securityClient.Users.GetUserClaimValuesAsync(
    claimsPrincipal,
    type: ClaimTypes.Role);

foreach (string role in roles)
    Console.WriteLine(role); // "Teacher", "Moderator"
```

---

## Testing with Mocks

Because your service depends on `ISecurityAuditBroker` (an interface), Moq can stand in for it during unit tests with zero HTTP context or JWT infrastructure needed.

### The Pass-Through Pattern

The key insight for testing under The Standard is that `ApplyAddAuditValuesAsync` (and its siblings) **transform** the entity before validation runs. If the mock returns the same object unchanged, validation may fail because fields like `CreatedBy` are still empty. The solution is to set up the mock to return a **pre-audited clone** that already carries the expected values — exactly as the real broker would return in production.

This lets you write your validation tests against the same rules the real service enforces, without needing a real security context.

```csharp
public partial class StudentServiceTests
{
    private readonly Mock<IStorageBroker> storageBrokerMock;
    private readonly Mock<IDateTimeBroker> dateTimeBrokerMock;
    private readonly Mock<ISecurityAuditBroker> securityAuditBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IStudentService studentService;

    public StudentServiceTests()
    {
        this.storageBrokerMock = new Mock<IStorageBroker>();
        this.dateTimeBrokerMock = new Mock<IDateTimeBroker>();
        this.securityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.studentService = new StudentService(
            storageBroker: this.storageBrokerMock.Object,
            dateTimeBroker: this.dateTimeBrokerMock.Object,
            securityAuditBroker: this.securityAuditBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }
}
```

### Logic Test — ShouldAddStudentAsync

This test demonstrates the full pass-through chain:

1. A random user ID and timestamp are generated.
2. An input `Student` is created whose `CreatedBy` / `CreatedDate` already match those values (as the real broker would set them).
3. The mock is told: *"when `ApplyAddAuditValuesAsync` is called with `inputStudent`, return `auditAppliedStudent`"* — mimicking what the real broker does.
4. Subsequent mocks chain off the audited version, so the whole flow is verified end-to-end.

```csharp
[Fact]
public async Task ShouldAddStudentAsync()
{
    // given
    string randomUserId = GetRandomString();
    DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

    // Build the input student with the audit values pre-populated,
    // matching what ApplyAddAuditValuesAsync would set in production.
    Student randomStudent = CreateStudentFiller(randomDateTimeOffset, randomUserId).Create();
    Student inputStudent = randomStudent;

    // The audited clone represents what the broker returns after stamping fields.
    Student auditAppliedStudent = inputStudent.DeepClone();
    auditAppliedStudent.CreatedBy   = randomUserId;
    auditAppliedStudent.CreatedWhen = randomDateTimeOffset;
    auditAppliedStudent.UpdatedBy   = randomUserId;
    auditAppliedStudent.UpdatedWhen = randomDateTimeOffset;

    Student storageStudent  = auditAppliedStudent.DeepClone();
    Student expectedStudent = storageStudent.DeepClone();

    // Wire up the security audit broker to pass through the audited object.
    // This is the critical setup: without it the service would receive an
    // un-audited entity and validation would fail on required audit fields.
    this.securityAuditBrokerMock
        .Setup(broker => broker.ApplyAddAuditValuesAsync(inputStudent))
        .ReturnsAsync(auditAppliedStudent);

    this.storageBrokerMock
        .Setup(broker => broker.InsertStudentAsync(
            auditAppliedStudent,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(storageStudent);

    // when
    Student actualStudent =
        await this.studentService.AddStudentAsync(
            inputStudent,
            TestContext.Current.CancellationToken);

    // then
    actualStudent.Should().BeEquivalentTo(expectedStudent);

    this.securityAuditBrokerMock.Verify(broker =>
        broker.ApplyAddAuditValuesAsync(inputStudent),
        Times.Once);

    this.storageBrokerMock.Verify(broker =>
        broker.InsertStudentAsync(auditAppliedStudent, It.IsAny<CancellationToken>()),
        Times.Once);

    this.securityAuditBrokerMock.VerifyNoOtherCalls();
    this.dateTimeBrokerMock.VerifyNoOtherCalls();
    this.storageBrokerMock.VerifyNoOtherCalls();
    this.loggingBrokerMock.VerifyNoOtherCalls();
}
```

### Validation Test — Checking Audit Fields

Because the mock returns a controlled audited object, Standard validation tests (e.g. "CreatedBy must match current user") can still be exercised normally. The test controls what the broker returns, so it can deliberately introduce an invalid value to trigger the expected exception:

```csharp
[Fact]
public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotSameAsCurrentUserIdAndLogItAsync()
{
    // given
    string randomUserId = GetRandomString();
    string differentUserId = GetRandomString(); // intentionally different
    DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

    Student randomStudent = CreateStudentFiller(randomDateTimeOffset, randomUserId).Create();
    Student inputStudent = randomStudent;

    // The broker returns the student, but with a DIFFERENT user in CreatedBy
    // than what GetUserIdAsync reports — triggering the tamper-detection rule.
    Student auditAppliedStudent = inputStudent.DeepClone();
    auditAppliedStudent.CreatedBy = differentUserId;

    var invalidStudentException = new InvalidStudentException(
        message: "Student is invalid, fix the errors and try again.");

    invalidStudentException.UpsertDataList(
        key: nameof(Student.CreatedBy),
        value: "CreatedBy is not the same as current user id");

    var expectedStudentValidationException = new StudentValidationException(
        message: "Student validation error occurred, fix the errors and try again.",
        innerException: invalidStudentException);

    this.securityAuditBrokerMock
        .Setup(broker => broker.GetUserIdAsync())
        .ReturnsAsync(randomUserId);

    // Pass through the tampered student — this is what triggers the validation failure.
    this.securityAuditBrokerMock
        .Setup(broker => broker.ApplyAddAuditValuesAsync(inputStudent))
        .ReturnsAsync(auditAppliedStudent);

    // when
    ValueTask<Student> addStudentTask =
        this.studentService.AddStudentAsync(
            inputStudent,
            TestContext.Current.CancellationToken);

    StudentValidationException actualStudentValidationException =
        await Assert.ThrowsAsync<StudentValidationException>(addStudentTask.AsTask);

    // then
    actualStudentValidationException.Should()
        .BeEquivalentTo(expectedStudentValidationException);

    this.securityAuditBrokerMock.Verify(broker =>
        broker.GetUserIdAsync(),
        Times.Once);

    this.securityAuditBrokerMock.Verify(broker =>
        broker.ApplyAddAuditValuesAsync(inputStudent),
        Times.Once);

    this.loggingBrokerMock.Verify(broker =>
        broker.LogErrorAsync(It.Is(
            SameExceptionAs(expectedStudentValidationException))),
        Times.Once);

    this.securityAuditBrokerMock.VerifyNoOtherCalls();
    this.dateTimeBrokerMock.VerifyNoOtherCalls();
    this.storageBrokerMock.VerifyNoOtherCalls();
    this.loggingBrokerMock.VerifyNoOtherCalls();
}
```

### Why This Works

| Without pass-through mock | With pass-through mock |
|---|---|
| Broker returns `null` or the unchanged input | Broker returns an entity with correct audit values stamped |
| Validation fails immediately on `CreatedBy == null` | Validation runs against meaningful, realistic data |
| You cannot distinguish between "audit not applied" and "validation rule violated" | Each test exercises exactly one failure mode, in isolation |

The mock setup is not bypassing validation — it is **standing in for the real broker** so validation can do its job on realistic data, exactly as The Standard requires.