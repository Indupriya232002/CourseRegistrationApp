
# Design Specification: Course Registration Waitlist Slice

### Architecture & Boundaries
- Built using a **multi-layer structure** (`Domain` and `Application`), separating core contracts from business service rules.

### Invariants
1. `EnrolledCount <= Capacity` at all times.
2. No duplicate learners across `Enrolled` or `Waitlist`.
3. Strict FIFO promotion order maintained for eligible waitlisted learners.

### Waitlist Ineligibility Policy
- **Chosen Policy:** **Skip Policy**. If an enrolled seat becomes available and the learner at the head of the waitlist currently fails the prerequisite rule, they are retained in the waitlist at their current position, and the system promotes the earliest subsequent eligible learner.

### Concurrency & Thread-Safety
- Synchronized using an internal monitor lock (`lock (_lock)`). All mutations (`Enroll`, `Drop`, and `PromoteNextEligible`) execute atomically, preventing lost updates or capacity race conditions under parallel requests.