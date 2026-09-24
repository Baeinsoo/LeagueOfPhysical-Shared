using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryFaceCoordsTests
    {
        //  사수는 +z를 보고, 과녁은 사수 쪽(−z)을 본다.
        [Test]
        public void 사수_기준_오른쪽_위가_양수()
        {
            var face = ArcheryFaceCoords.ToFaceOffset(new Vector3(0.3f, 0.1f, 0f), Vector3.back, 1f);
            Assert.AreEqual(0.3f, face.x, 1e-5f);
            Assert.AreEqual(0.1f, face.y, 1e-5f);
        }

        [Test]
        public void 반지름으로_나눈다()
        {
            var face = ArcheryFaceCoords.ToFaceOffset(new Vector3(0.3f, 0f, 0f), Vector3.back, 0.6f);
            Assert.AreEqual(0.5f, face.x, 1e-5f);
        }
    }
}
