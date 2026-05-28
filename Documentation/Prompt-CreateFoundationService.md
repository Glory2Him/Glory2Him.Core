You are working in a repository that contains project skills, templates, and standards documentation.
Your task is to create a The Standard compliant Foundation Service by following the repository skills and templates exactly.
You MUST follow strict TDD Red → Green practices, repository standards, and the implementation sequence defined below.
Commit history is critical and will be reviewed.
Core Rules
Repository Compliance
	• Follow the skills in this repository exactly.
	• Follow the repository templates exactly.
	• Adhere to The Standard architecture and repository conventions.
	• Use the existing Foundation Service patterns already present in the repo.
	• Reuse repository implementations and conventions wherever possible.
Testing Rules
	• Anything that can be tested MUST be tested.
	• Follow strict TDD Red → Green practices.
	• Create exactly ONE test at a time.
	• Never batch multiple tests together.
	• Never implement future tests ahead of sequence.
	• Run tests after every change.
Implementation Rules
	• ONLY implement the absolute minimum code required for the current step.
	• Do not over-engineer.
	• Do not future-proof.
	• Do not implement code for later phases.
	• Do not implement additional logic because it "might be needed later".
	• If uncertain, stop and ask rather than assume.
Progression Rules
	• Never continue automatically.
	• Stop at every review checkpoint.
	• Wait for me to:
		○ request corrections, OR
		○ reply with PROCEED
	• Never move to the next phase without approval.
	• Never move to the next CRUD operation without approval.

Required Implementation Order
Work in vertical slices per CRUD operation.
You MUST fully complete one CRUD operation before moving to the next.
You MUST NOT move to another CRUD operation until the current CRUD operation has completed:
	1. Logic tests
	2. Validation tests
	3. Exception tests

CRUD Order
Complete CRUD operations in this exact order:
	1. Add
	2. RetrieveAll
	3. RetrieveById
	4. Modify
	5. RemoveById
Do not change this order.

Execution Process
For every single test, follow this exact sequence.
Phase 1 — RED
Step 1 — Create ONE failing test only
Create exactly ONE failing test only for the current phase.
Do not create additional tests.
Step 2 — Run the test
Run the relevant test.
Step 3 — Verify expected failure
Verify the test fails for the expected reason.
If the failure reason is incorrect:
	• Fix the test
	• Rerun the test
	• Repeat until the failure reason is correct
Do not continue until the failure reason is correct.
Step 4 — Review checkpoint
Stop and wait for review.
Wait for:
	• corrections, OR
	• PROCEED
Do not continue automatically.
Step 5 — FAIL commit
After approval, create a local commit:
%testname% -> FAIL

Phase 2 — GREEN
Step 6 — Implement minimum production code
Implement the absolute minimum code required to make the single test pass.
You must:
	• change as little production code as possible
	• avoid unnecessary refactoring
	• avoid unrelated improvements
	• avoid future-proofing
Step 7 — Run tests
Run the relevant test(s).
Step 8 — Verify success
Verify the test passes.
If the test fails:
	• fix only what is necessary
	• rerun the tests
Step 9 — Review checkpoint
Stop and wait for review.
Wait for:
	• corrections, OR
	• PROCEED
Do not continue automatically.
Step 10 — PASS commit
After approval, create a local commit:
%testname% -> PASS
Step 11 — Continue
Repeat this process for the next required test in sequence.

Logic Phase Rules
For each CRUD operation:
Start with the logic test phase.
Rules:
	• Create exactly ONE logic test at a time
	• Implement only the minimum logic required
	• TryCatch is forbidden
	• No validation logic
	• No exception handling
	• No dependency exception handling
	• No service exception handling
You MUST NOT:
	• add TryCatch
	• add validations
	• add exception handling
	• add orchestration logic
	• implement future CRUD functionality
If logic implementation appears to require any of the above:
Stop and explain why before proceeding.

Validation Phase Rules
Only begin validation tests after the CRUD operation logic test has completed successfully.
Validation tests must be completed one at a time and in this exact order:
	1. Null validation
	2. Required field validation (if applicable)
	3. Maximum length validation (if applicable)
	4. Minimum length validation (if applicable)
Rules:
	• Implement only the validation required for the current test
	• Do not implement future validations
	• Do not implement exception handling yet
	• Do not move to the exception phase early

Exception Phase Rules
Only begin exception tests after all validation tests for the CRUD operation are complete.
Exception tests must be completed one at a time and in this exact order:
	1. Dependency exception test
	2. Dependency validation exception test
	3. Service exception test
Rules:
	• Implement only the minimum exception handling required
	• TryCatch may ONLY be added during this phase
	• Do not add unrelated exception handling
	• Do not implement exception handling for future CRUD operations

Vertical Slice Rule
You MUST complete all phases for a CRUD operation before moving to the next CRUD operation.
Correct sequence example:
	1. Add logic
	2. Add validations
	3. Add exceptions
	4. Retrieve logic
	5. Retrieve validations
	6. Retrieve exceptions
	7. Modify logic
	8. Modify validations
	9. Modify exceptions
	10. Remove logic
	11. Remove validations
	OperationCanceledException when operationCanceledException.CancellationToken.IsCancellationRequested is false
	12. OperationCanceledException
	13. Remove exceptions
Incorrect sequence example:
	1. Add logic
	2. Retrieve logic
	3. Modify logic
	4. Remove logic
This is forbidden.

Commit Rules
Every failing state requires a FAIL commit:
%testname% -> FAIL
Every passing state requires a PASS commit:
%testname% -> PASS
Commits must reflect actual TDD progression.
Do not squash steps.
Do not skip commits.

Final Constraint
If repository skills, templates, or standards conflict with assumptions, the repository is authoritative.
If uncertain:
STOP AND ASK.
Begin by:
	1. Identifying the correct Foundation Service template from the repository skills.
	2. Creating ONE failing logic test only for the first CRUD operation (Insert).
	3. Running the test and verifying it fails for the expected reason.
	4. Stopping for review.
