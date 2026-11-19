using System;
using FsCheck;
using FsCheck.Fluent;
using Online_Course_Enrollment_System_Properties.Domain;

namespace Online_Course_Enrollment_System_Properties.Tests.Arbitraries
{
    public static class CourseArbitraries
    {
        private static Gen<string> StudentIdGen()
        {
            var stIdGen = Gen.Choose(1, 30).Select(n => $"STDNT-{n}-ID");

            return stIdGen;
        }

        private static Gen<string> CourseIdGen()
        {
            var crsIdGen = Gen.Choose(1, 50).Select(n => $"CRS-{n}-ID");

            return crsIdGen;
        }

        public static Arbitrary<Student> Student()
        {
            var gen = from id in StudentIdGen()
                      select new Student(id);

            return gen.ToArbitrary();
        }

        public static Arbitrary<Course> Course()
        {
            var capacityGen = Gen.Choose(1, 25);

            var gen = from id in CourseIdGen()
                      from capacity in capacityGen
                      select new Course(id, capacity);

            return gen.ToArbitrary();
        }
    }
}
