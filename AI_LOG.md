# AI Prompt and Use Record

### 1. Queue Architecture & State Invariants
- **Prompt:** "Design an in-memory thread-safe Course Offering service in C# that supports enroll, drop, and FIFO waitlist promotion. What data structures prevent duplicates and give O(1) performance?"
- **AI Response:** Suggested using `List<string>` for both enrolled and waitlisted learners with a basic `lock`.
- **Review Decision:** **Edited**. A plain list requires O(N) lookup to check duplicate enrollment. Modified `enrolled` to `HashSet<string>` for O(1) duplicate checks and used `LinkedList<string>` for the waitlist so skipping/removing nodes from arbitrary positions during promotion is O(1).
- **Verification:** Verified via `DuplicateRequests_DoNotCreateDuplicates` and `Concurrency_ThreadSafetyUnderContention` unit tests.

### 2. Waitlist Eligibility & Drop Policy
- **Prompt:** "Write the Drop method logic in C# that automatically promotes the head of the waitlist. If the head does not meet prerequisites, should we pop or skip?"
- **AI Response:** Provided code doing `_waitlist.Dequeue()` unconditionally.
- **Review Decision:** **Rejected**. Unconditionally popping the head violates the requirement to enforce a consistent ineligible learner policy. Implemented an explicit "Skip Policy" which traverses the FIFO queue, skips ineligible learners, and promotes the next eligible one while leaving skipped learners in line.
- **Verification:** Verified via `IneligibleLearnerAtFrontOfWaitlist_IsSkippedConsistently` unit test.