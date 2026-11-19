using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Online_Course_Enrollment_System_Properties.Domain;
using Online_Course_Enrollment_System_Properties.Tests.Arbitraries;

namespace Online_Course_Enrollment_System_Properties.Tests.Properties
{
    public class CourseProperties
    {
        private static Course CloneCourse(Course source)
        {
            var copy = new Course(source.Id, source.Capacity);
            foreach (var student in source.Students)
            {
                copy.Enroll(new Student(student.Id));
            }
            return copy;
        }

        //P1 · Туди й назад
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Enroll_then_unenroll_restores_initial_state(Course course, Student[] initialStudents, Student target)
        {
            foreach (var s in initialStudents)
                course.Enroll(s);

            bool correctForTest = !course.IsFull && !course.IsEnrolled(target.Id);
            bool ok = true;

            if (correctForTest)
            {
                int seatsBefore = course.FreeSeats;
                int countBefore = course.Students.Count;

                bool enrolled = course.Enroll(target);
                course.Unenroll(target.Id);

                ok = enrolled
                     && !course.IsEnrolled(target.Id)
                     && course.FreeSeats == seatsBefore
                     && course.Students.Count == countBefore;
            }

            return ok.When(correctForTest);
        }

        //P2 · Інваріант
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Course_enrollment_invariants_hold_for_any_students(Course course, Student[] students)
        {
            foreach (var student in students)
            {
                course.Enroll(student);

                course.Students.Count.Should().BeGreaterThanOrEqualTo(0);
                course.Students.Count.Should().BeLessThanOrEqualTo(course.Capacity);
                course.FreeSeats.Should().Be(course.Capacity - course.Students.Count);
                course.IsFull.Should().Be(course.Students.Count == course.Capacity);
                course.Students.Select(s => s.Id).Should().OnlyHaveUniqueItems();
            }
        }

        //P3 · Ідемпотентність
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Enrolling_same_student_twice_is_idempotent(Course course, Student student)
        {
            bool firstEnrolled = course.Enroll(student);
            bool ok = true;

            if (firstEnrolled)
            {
                int countAfterFirst = course.Students.Count;
                int seatsAfterFirst = course.FreeSeats;

                bool secondEnrolled = course.Enroll(student);

                ok = !secondEnrolled
                     && course.Students.Count == countAfterFirst
                     && course.FreeSeats == seatsAfterFirst;
            }

            return ok.When(firstEnrolled);
        }

        //P4 · Різні шляхи
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Enrolling_two_students_in_different_order_yields_same_state(Course course, Student s1, Student s2)
        {
            bool correctForTest = course.Capacity >= 2 && s1.Id != s2.Id;
            bool ok = true;

            if (correctForTest)
            {
                var courseOrder1 = CloneCourse(course);
                var courseOrder2 = CloneCourse(course);

                //Шлях 1: спочатку s1, потім s2
                bool r1First = courseOrder1.Enroll(s1);
                bool r1Second = courseOrder1.Enroll(s2);

                //Шлях 2: спочатку s2, потім s1
                bool r2First = courseOrder2.Enroll(s2);
                bool r2Second = courseOrder2.Enroll(s1);

                ok = r1First && r1Second
                     && r2First && r2Second
                     && courseOrder1.FreeSeats == courseOrder2.FreeSeats
                     && courseOrder1.Students.Count == courseOrder2.Students.Count
                     && courseOrder1.Students.All(s => courseOrder2.IsEnrolled(s.Id));
            }

            return ok.When(correctForTest);
        }

        //P8 · Без винятків
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Enroll_valid_student_never_throws(Course course, Student[] students)
        {
            Action act = () =>
            {
                foreach (var s in students)
                {
                    course.Enroll(s);
                }
            };

            act.Should().NotThrow();
        }

        //Перевірка винятку (Course.Unenroll => InvalidOperationException)
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Unenroll_non_enrolled_student_throws(Course course, Student[] enrolledStudents, Student notEnrolledStudent)
        {
            foreach (var s in enrolledStudents)
                course.Enroll(s);

            bool correctForTest = !course.IsEnrolled(notEnrolledStudent.Id);
            bool ok = true;

            if (correctForTest)
            {
                Action act = () => course.Unenroll(notEnrolledStudent.Id);
                act.Should().Throw<InvalidOperationException>();
            }

            return ok.When(correctForTest);
        }
    }
}
