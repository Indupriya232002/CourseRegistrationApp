using System.Collections.Generic;
using System.Threading.Tasks;
using CourseRegistration.Application;
using CourseRegistration.Domain;
using Xunit;

namespace CourseRegistration.Tests
{
    public class CourseServiceTests
    {
        private readonly Dictionary<string, HashSet<string>> _studentPrereqs = new Dictionary<string, HashSet<string>>
        {
            { "alice", new HashSet<string> { "CS101" } },
            { "bob", new HashSet<string> { "CS101" } },
            { "charlie", new HashSet<string> { "CS101" } },
            { "david", new HashSet<string> { "CS101" } },
            { "eve_ineligible", new HashSet<string>() }
        };

        private CourseService CreateService(int capacity = 2)
        {
            return new CourseService(capacity, "CS101", id => 
                _studentPrereqs.ContainsKey(id) ? _studentPrereqs[id] : new HashSet<string>());
        }

        [Fact]
        public void AvailableCapacity_EnrollSucceeds()
        {
            var service = CreateService(capacity: 2);
            var result = service.Enroll("alice");

            Assert.Equal(RegistrationResult.Enrolled, result);
            Assert.Contains("alice", service.EnrolledLearners);
        }

        [Fact]
        public void FullCapacity_EligibleLearnerJoinsWaitlist()
        {
            var service = CreateService(capacity: 1);
            service.Enroll("alice");

            var result = service.Enroll("bob");

            Assert.Equal(RegistrationResult.Waitlisted, result);
            Assert.Contains("bob", service.WaitlistedLearners);
        }

        [Fact]
        public void DuplicateRequests_DoNotCreateDuplicates()
        {
            var service = CreateService(capacity: 2);
            service.Enroll("alice");

            var duplicateResult = service.Enroll("alice");

            Assert.Equal(RegistrationResult.AlreadyEnrolled, duplicateResult);
            Assert.Single(service.EnrolledLearners);
        }

        [Fact]
        public void PrerequisiteFailure_CannotEnrollOrWaitlist()
        {
            var service = CreateService(capacity: 2);
            var result = service.Enroll("eve_ineligible");

            Assert.Equal(RegistrationResult.PrerequisiteFailed, result);
            Assert.DoesNotContain("eve_ineligible", service.EnrolledLearners);
            Assert.DoesNotContain("eve_ineligible", service.WaitlistedLearners);
        }

        [Fact]
        public void DropEnrolled_PromotesEarliestWaitlistedLearner_FIFO()
        {
            var service = CreateService(capacity: 1);
            service.Enroll("alice");
            service.Enroll("bob");
            service.Enroll("charlie");

            var dropResult = service.Drop("alice");

            Assert.Equal(RegistrationResult.DropSuccessful, dropResult);
            Assert.Contains("bob", service.EnrolledLearners);
            Assert.DoesNotContain("bob", service.WaitlistedLearners);
            Assert.Contains("charlie", service.WaitlistedLearners);
        }

        [Fact]
        public void IneligibleLearnerAtFrontOfWaitlist_IsSkippedConsistently()
        {
            var service = CreateService(capacity: 1);
            service.Enroll("alice");
            service.Enroll("bob");
            service.Enroll("charlie");
            _studentPrereqs["bob"].Clear();

            service.Drop("alice");

            Assert.Contains("charlie", service.EnrolledLearners);
            Assert.Contains("bob", service.WaitlistedLearners);
        }

        [Fact]
        public void InvalidOrRepeatedDrops_DoNotCorruptState()
        {
            var service = CreateService(capacity: 2);
            service.Enroll("alice");

            var invalidDrop = service.Drop("ghost_user");
            Assert.Equal(RegistrationResult.NotEnrolled, invalidDrop);

            var firstDrop = service.Drop("alice");
            Assert.Equal(RegistrationResult.DropSuccessful, firstDrop);

            var repeatedDrop = service.Drop("alice");
            Assert.Equal(RegistrationResult.NotEnrolled, repeatedDrop);
        }

        [Fact]
        public void Concurrency_ThreadSafetyUnderContention()
        {
            var service = CreateService(capacity: 5);
            var learners = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                var id = $"learner_{i}";
                _studentPrereqs[id] = new HashSet<string> { "CS101" };
                learners.Add(id);
            }

            Parallel.ForEach(learners, id => service.Enroll(id));

            Assert.Equal(5, service.EnrolledLearners.Count);
            Assert.Equal(15, service.WaitlistedLearners.Count);
        }
    }
}