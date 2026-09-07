using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 그리는 상자(기즈모·콜라이더)가 <b>판정이 미는 방향</b>과 나란한지 잰다.
    ///
    /// <para>쿼터니언끼리 비교하면 기하가 아니라 <i>규약</i>을 재는 것이 된다 — 90°의 배수에서는
    /// 부호가 반대여도 상자가 자기 자신으로 겹쳐 어느 쪽이든 통과한다. 그래서 90°가 아닌 각을
    /// 함께 넣고, 축 방향을 <see cref="DoorGeometry.PanelCenter"/>에서 직접 뽑아 맞춰 본다.</para>
    /// </summary>
    public class DoorPanelRotationTests
    {
        private static Door DoorAt(float axisAngleDegrees) => new Door(
            System.Numerics.Vector3.Zero,
            halfWidth: 8f, halfDepth: 8f, thickness: 2.8f,
            axisAngle: axisAngleDegrees * Mathf.Deg2Rad,
            period: 120, openTicks: 40, moveTicks: 20, phase: 0);

        // 판정이 패널을 실제로 밀어내는 방향. 각도 식을 여기서 다시 쓰지 않고 결과에서 뽑는다.
        private static Vector3 SlideAxis(float axisAngleDegrees)
        {
            Door door = DoorAt(axisAngleDegrees);
            System.Numerics.Vector3 offset = DoorGeometry.PanelCenter(door, 1, 1f) - door.Center;
            return new Vector3(offset.X, offset.Y, offset.Z).normalized;
        }

        [TestCase(0f)]
        [TestCase(45f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(-30f)]
        public void 패널_상자의_긴_축이_판정이_미는_방향과_나란하다(float axisAngleDegrees)
        {
            Vector3 boxAxis = DoorVolume.PanelRotation(axisAngleDegrees) * Vector3.right;

            //  상자는 중심 대칭이라 앞뒤가 같은 부피다 — 절댓값으로 잰다.
            Assert.AreEqual(1f, Mathf.Abs(Vector3.Dot(boxAxis, SlideAxis(axisAngleDegrees))), 0.001f,
                            $"{axisAngleDegrees}도");
        }

        [TestCase(45f)]
        [TestCase(-30f)]
        public void 패널_상자의_두께_방향은_세로다(float axisAngleDegrees)
        {
            //  Y축 회전만 걸어야 두께가 세로로 선다. 다른 축이 섞이면 상자가 누워
            //  얇은 면이 옆을 향한다 — 크기만 재는 검사는 그것을 못 본다.
            Vector3 boxUp = DoorVolume.PanelRotation(axisAngleDegrees) * Vector3.up;

            Assert.AreEqual(1f, Vector3.Dot(boxUp, Vector3.up), 0.001f, $"{axisAngleDegrees}도");
        }
    }
}
