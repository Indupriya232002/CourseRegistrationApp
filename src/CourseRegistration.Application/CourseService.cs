using System;
using System.Collections.Generic;
using System.Linq;
using CourseRegistration.Domain;

namespace CourseRegistration.Application
{
    public class CourseService
    {
        private readonly int _capacity;
        private readonly string _requiredPrerequisite;
        private readonly object _lock = new object();

        private readonly HashSet<string> _enrolled = new HashSet<string>();
        private readonly LinkedList<string> _waitlist = new LinkedList<string>();
        private readonly Func<string, HashSet<string>> _prerequisiteLookup;

        public CourseService(int capacity, string requiredPrerequisite, Func<string, HashSet<string>> prerequisiteLookup)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive.");
            _capacity = capacity;
            _requiredPrerequisite = requiredPrerequisite;
            _prerequisiteLookup = prerequisiteLookup ?? (_ => new HashSet<string>());
        }

        public IReadOnlyCollection<string> EnrolledLearners
        {
            get { lock (_lock) { return _enrolled.ToList().AsReadOnly(); } }
        }

        public IReadOnlyCollection<string> WaitlistedLearners
        {
            get { lock (_lock) { return _waitlist.ToList().AsReadOnly(); } }
        }

        public RegistrationResult Enroll(string learnerId)
        {
            if (string.IsNullOrWhiteSpace(learnerId))
                throw new ArgumentException("Learner ID cannot be empty.");

            lock (_lock)
            {
                if (_enrolled.Contains(learnerId))
                    return RegistrationResult.AlreadyEnrolled;

                if (_waitlist.Contains(learnerId))
                    return RegistrationResult.AlreadyWaitlisted;

                if (!HasSatisfiedPrerequisite(learnerId))
                    return RegistrationResult.PrerequisiteFailed;

                if (_enrolled.Count < _capacity)
                {
                    _enrolled.Add(learnerId);
                    return RegistrationResult.Enrolled;
                }

                _waitlist.AddLast(learnerId);
                return RegistrationResult.Waitlisted;
            }
        }

        public RegistrationResult Drop(string learnerId)
        {
            if (string.IsNullOrWhiteSpace(learnerId))
                throw new ArgumentException("Learner ID cannot be empty.");

            lock (_lock)
            {
                if (!_enrolled.Contains(learnerId))
                    return RegistrationResult.NotEnrolled;

                _enrolled.Remove(learnerId);
                PromoteNextEligible();

                return RegistrationResult.DropSuccessful;
            }
        }

        private void PromoteNextEligible()
        {
            if (_enrolled.Count >= _capacity || _waitlist.Count == 0)
                return;

            var currentNode = _waitlist.First;
            while (currentNode != null)
            {
                var candidateId = currentNode.Value;
                if (HasSatisfiedPrerequisite(candidateId))
                {
                    _waitlist.Remove(currentNode);
                    _enrolled.Add(candidateId);
                    break;
                }
                currentNode = currentNode.Next;
            }
        }

        private bool HasSatisfiedPrerequisite(string learnerId)
        {
            if (string.IsNullOrEmpty(_requiredPrerequisite)) return true;
            var completed = _prerequisiteLookup(learnerId);
            return completed != null && completed.Contains(_requiredPrerequisite);
        }
    }
}