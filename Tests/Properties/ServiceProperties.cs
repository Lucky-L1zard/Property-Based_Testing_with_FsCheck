using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Online_Course_Enrollment_System_Properties.Domain;
using Online_Course_Enrollment_System_Properties.Tests.Arbitraries;

namespace Online_Course_Enrollment_System_Properties.Tests.Properties
{
    public class ServiceProperties
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

        //P5 · Оракул
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Service_enroll_matches_direct_course_enroll(Course baseCourse, Student[] students)
        {
            //Передбачені правильні значення: прямий виклик методів об'єкта Course
            var directCourse = CloneCourse(baseCourse);

            //На перевірку: виклик через EnrollmentService + EnrollmentRepository
            var repo = new EnrollmentRepository();
            repo.Save(CloneCourse(baseCourse));
            var service = new EnrollmentService(repo);

            foreach (var s in students)
            {
                bool directResult = directCourse.Enroll(s);
                bool serviceResult = service.Enroll(baseCourse.Id, s);

                serviceResult.Should().Be(directResult);
            }

            var storedCourse = repo.Get(baseCourse.Id);
            storedCourse.Students.Should().BeEquivalentTo(directCourse.Students);
            storedCourse.FreeSeats.Should().Be(directCourse.FreeSeats);
        }

        //P7 · Легко перевірити
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Total_enrollments_equals_sum_of_student_courses(Course[] courses, Student[] students)
        {
            var repo = new EnrollmentRepository();
            var service = new EnrollmentService(repo);

            foreach (var c in courses)
            {
                if (!repo.Exists(c.Id))
                    repo.Save(CloneCourse(c));
            }

            var uniqueCourseIds = repo.All().Select(c => c.Id).ToList();

            for (int i = 0; i < students.Length && uniqueCourseIds.Count > 0; i++)
            {
                var courseId = uniqueCourseIds[i % uniqueCourseIds.Count];
                service.Enroll(courseId, students[i]);
            }

            var distinctStudentIds = students.Select(s => s.Id).Distinct();
            int sumByStudents = distinctStudentIds.Sum(id => service.CoursesOf(id));

            service.TotalEnrollments().Should().Be(sumByStudents);
        }

        //P3 · Ідемпотентність
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Repository_save_same_course_id_overwrites_previous(Course initialCourse, Course secondCourse)
        {
            var updatedCourse = new Course(initialCourse.Id, secondCourse.Capacity);
            var repo = new EnrollmentRepository();

            repo.Save(initialCourse);
            repo.Save(updatedCourse);

            repo.All().Should().ContainSingle(c => c.Id == initialCourse.Id);
            repo.Get(initialCourse.Id).Capacity.Should().Be(secondCourse.Capacity);
        }

        //Перевірка винятку (EnrollmentService.CreateCourse => InvalidOperationException)
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public void Create_duplicate_course_throws(Course course, Course otherCourse)
        {
            var repo = new EnrollmentRepository();
            var service = new EnrollmentService(repo);

            service.CreateCourse(course.Id, course.Capacity);

            Action act = () => service.CreateCourse(course.Id, otherCourse.Capacity);
            act.Should().Throw<InvalidOperationException>();
        }

        // Перевірка винятку (EnrollmentRepository.Get => KeyNotFoundException)
        [Property(Arbitrary = new[] { typeof(CourseArbitraries) })]
        public Property Get_non_existent_course_throws(Course[] existingCourses, Course target)
        {
            var repo = new EnrollmentRepository();
            foreach (var c in existingCourses)
                repo.Save(CloneCourse(c));

            bool correctForTest = !repo.Exists(target.Id);
            bool ok = true;

            if (correctForTest)
            {
                Action act = () => repo.Get(target.Id);
                act.Should().Throw<KeyNotFoundException>();
            }

            return ok.When(correctForTest);
        }
    }
}