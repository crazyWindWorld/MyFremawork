using System.Collections.Generic;
using UnityEngine;
namespace Fuel.Tools
{
    public static class BezierUtil
    {
        /// <summary>
        /// 3D 自动补控制点时使用的弯曲平面
        /// </summary>
        public enum BezierPlane
        {
            XY,
            XZ,
            YZ
        }

        #region Vector3 贝塞尔曲线

        /// <summary>
        /// 根据控制点生成多阶贝塞尔曲线点。
        /// 如果只传入 2 个点，则自动补一个控制点：
        /// 在起点和终点中点处，沿指定平面的垂直方向偏移 lineLength * autoOffsetFactor。
        /// </summary>
        /// <param name="controlPoints">控制点列表，至少需要 2 个点</param>
        /// <param name="pointCount">生成点数量，至少为 2</param>
        /// <param name="autoOffsetFactor">自动补点时的偏移比例，默认 0.25</param>
        /// <param name="positiveSide">自动补点时偏移方向</param>
        /// <param name="plane">3D 自动补点时使用的平面</param>
        public static List<Vector3> GenerateBezierPoints(
            IList<Vector3> controlPoints,
            int pointCount,
            float autoOffsetFactor = 0.25f,
            bool positiveSide = true,
            BezierPlane plane = BezierPlane.XZ)
        {
            List<Vector3> result = new List<Vector3>();

            if (controlPoints == null || controlPoints.Count < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateBezierPoints: 控制点数量至少需要 2 个");
                return result;
            }

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateBezierPoints: 生成点数量至少需要 2 个");
                return result;
            }

            IList<Vector3> actualControlPoints = controlPoints;

            // 如果只有起点和终点，自动补一个控制点，生成二阶贝塞尔曲线
            if (controlPoints.Count == 2)
            {
                Vector3 p0 = controlPoints[0];
                Vector3 p2 = controlPoints[1];
                Vector3 p1 = GetAutoControlPoint3D(p0, p2, autoOffsetFactor, positiveSide, plane);

                actualControlPoints = new List<Vector3> { p0, p1, p2 };
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector3 point = GetBezierPoint(actualControlPoints, t);
                result.Add(point);
            }

            return result;
        }

        /// <summary>
        /// 获取多阶贝塞尔曲线在 t 时刻的点
        /// t 范围：[0, 1]
        /// </summary>
        public static Vector3 GetBezierPoint(
            IList<Vector3> controlPoints,
            float t)
        {
            if (controlPoints == null || controlPoints.Count == 0)
            {
                return Vector3.zero;
            }

            t = Mathf.Clamp01(t);

            // 使用 De Casteljau 算法，支持任意阶贝塞尔曲线
            List<Vector3> tempPoints = new List<Vector3>(controlPoints);

            int count = tempPoints.Count;
            for (int level = 1; level < count; level++)
            {
                for (int i = 0; i < count - level; i++)
                {
                    tempPoints[i] = Vector3.Lerp(tempPoints[i], tempPoints[i + 1], t);
                }
            }

            return tempPoints[0];
        }

        /// <summary>
        /// 当只有起点终点时，自动生成一个控制点。
        /// 控制点位于中点，再沿指定平面的垂直方向偏移 lineLength * offsetFactor。
        /// </summary>
        public static Vector3 GetAutoControlPoint3D(
            Vector3 start,
            Vector3 end,
            float offsetFactor = 0.25f,
            bool positiveSide = true,
            BezierPlane plane = BezierPlane.XZ)
        {
            Vector3 dir = end - start;
            float length = dir.magnitude;

            if (length <= Mathf.Epsilon)
            {
                return start;
            }

            Vector3 mid = (start + end) * 0.5f;
            Vector3 dirNormalized = dir / length;

            Vector3 perpendicular = GetPerpendicularInPlane(dirNormalized, plane, positiveSide);
            float offsetDistance = length * offsetFactor;

            return mid + perpendicular * offsetDistance;
        }

        /// <summary>
        /// 获取方向向量在指定平面内的垂直向量
        /// </summary>
        private static Vector3 GetPerpendicularInPlane(
            Vector3 dirNormalized,
            BezierPlane plane,
            bool positiveSide)
        {
            Vector3 perpendicular;

            switch (plane)
            {
                case BezierPlane.XY:
                    {
                        // 在 XY 平面中，与 (x, y, 0) 垂直的方向可取 (-y, x, 0)
                        perpendicular = positiveSide
                            ? new Vector3(-dirNormalized.y, dirNormalized.x, 0f)
                            : new Vector3(dirNormalized.y, -dirNormalized.x, 0f);
                        break;
                    }
                case BezierPlane.YZ:
                    {
                        // 在 YZ 平面中，与 (0, y, z) 垂直的方向可取 (0, -z, y)
                        perpendicular = positiveSide
                            ? new Vector3(0f, -dirNormalized.z, dirNormalized.y)
                            : new Vector3(0f, dirNormalized.z, -dirNormalized.y);
                        break;
                    }
                case BezierPlane.XZ:
                default:
                    {
                        // 在 XZ 平面中，与 (x, 0, z) 垂直的方向可取 (-z, 0, x)
                        perpendicular = positiveSide
                            ? new Vector3(-dirNormalized.z, 0f, dirNormalized.x)
                            : new Vector3(dirNormalized.z, 0f, -dirNormalized.x);
                        break;
                    }
            }

            // 防止方向不在指定平面内时出现结果长度异常
            if (perpendicular.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.zero;
            }

            return perpendicular.normalized;
        }

        #endregion

        #region Vector2 贝塞尔曲线

        /// <summary>
        /// 根据 Vector2 控制点生成多阶贝塞尔曲线点。
        /// 如果只传入 2 个点，则自动补一个控制点：
        /// 在起点和终点中点处，沿垂直方向偏移 lineLength * autoOffsetFactor。
        /// </summary>
        public static List<Vector2> GenerateBezierPoints2D(
            IList<Vector2> controlPoints,
            int pointCount,
            float autoOffsetFactor = 0.25f,
            bool positiveSide = true)
        {
            List<Vector2> result = new List<Vector2>();

            if (controlPoints == null || controlPoints.Count < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateBezierPoints2D: 控制点数量至少需要 2 个");
                return result;
            }

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateBezierPoints2D: 生成点数量至少需要 2 个");
                return result;
            }

            IList<Vector2> actualControlPoints = controlPoints;

            // 如果只有起点和终点，自动补一个控制点
            if (controlPoints.Count == 2)
            {
                Vector2 p0 = controlPoints[0];
                Vector2 p2 = controlPoints[1];
                Vector2 p1 = GetAutoControlPoint2D(p0, p2, autoOffsetFactor, positiveSide);

                actualControlPoints = new List<Vector2> { p0, p1, p2 };
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                Vector2 point = GetBezierPoint2D(actualControlPoints, t);
                result.Add(point);
            }

            return result;
        }

        /// <summary>
        /// 获取 Vector2 多阶贝塞尔曲线在 t 时刻的点
        /// </summary>
        public static Vector2 GetBezierPoint2D(
            IList<Vector2> controlPoints,
            float t)
        {
            if (controlPoints == null || controlPoints.Count == 0)
            {
                return Vector2.zero;
            }

            t = Mathf.Clamp01(t);

            List<Vector2> tempPoints = new List<Vector2>(controlPoints);

            int count = tempPoints.Count;
            for (int level = 1; level < count; level++)
            {
                for (int i = 0; i < count - level; i++)
                {
                    tempPoints[i] = Vector2.Lerp(tempPoints[i], tempPoints[i + 1], t);
                }
            }

            return tempPoints[0];
        }

        /// <summary>
        /// 2D 自动生成控制点：
        /// 控制点位于中点，再沿垂直方向偏移 lineLength * offsetFactor。
        /// </summary>
        public static Vector2 GetAutoControlPoint2D(
            Vector2 start,
            Vector2 end,
            float offsetFactor = 0.25f,
            bool positiveSide = true)
        {
            Vector2 dir = end - start;
            float length = dir.magnitude;

            if (length <= Mathf.Epsilon)
            {
                return start;
            }

            Vector2 mid = (start + end) * 0.5f;
            Vector2 dirNormalized = dir / length;

            Vector2 perpendicular = positiveSide
                ? new Vector2(-dirNormalized.y, dirNormalized.x)
                : new Vector2(dirNormalized.y, -dirNormalized.x);

            float offsetDistance = length * offsetFactor;

            return mid + perpendicular * offsetDistance;
        }

        #endregion

        #region 常用二阶 / 三阶贝塞尔快捷方法

        /// <summary>
        /// 二阶贝塞尔曲线
        /// controlPoints: p0, p1, p2
        /// </summary>
        public static Vector3 GetQuadraticBezierPoint(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            float t)
        {
            t = Mathf.Clamp01(t);

            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * p0
                 + 2f * oneMinusT * t * p1
                 + t * t * p2;
        }

        /// <summary>
        /// 三阶贝塞尔曲线
        /// controlPoints: p0, p1, p2, p3
        /// </summary>
        public static Vector3 GetCubicBezierPoint(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t)
        {
            t = Mathf.Clamp01(t);

            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * oneMinusT * p0
                 + 3f * oneMinusT * oneMinusT * t * p1
                 + 3f * oneMinusT * t * t * p2
                 + t * t * t * p3;
        }

        /// <summary>
        /// 生成二阶贝塞尔曲线点
        /// </summary>
        public static List<Vector3> GenerateQuadraticBezierPoints(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            int pointCount)
        {
            List<Vector3> result = new List<Vector3>();

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateQuadraticBezierPoints: 生成点数量至少需要 2 个");
                return result;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                result.Add(GetQuadraticBezierPoint(p0, p1, p2, t));
            }

            return result;
        }

        /// <summary>
        /// 生成三阶贝塞尔曲线点
        /// </summary>
        public static List<Vector3> GenerateCubicBezierPoints(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            int pointCount)
        {
            List<Vector3> result = new List<Vector3>();

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateCubicBezierPoints: 生成点数量至少需要 2 个");
                return result;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                result.Add(GetCubicBezierPoint(p0, p1, p2, p3, t));
            }

            return result;
        }

        /// <summary>
        /// 2D 二阶贝塞尔曲线
        /// </summary>
        public static Vector2 GetQuadraticBezierPoint2D(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            float t)
        {
            t = Mathf.Clamp01(t);

            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * p0
                 + 2f * oneMinusT * t * p1
                 + t * t * p2;
        }

        /// <summary>
        /// 2D 三阶贝塞尔曲线
        /// </summary>
        public static Vector2 GetCubicBezierPoint2D(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            float t)
        {
            t = Mathf.Clamp01(t);

            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * oneMinusT * p0
                 + 3f * oneMinusT * oneMinusT * t * p1
                 + 3f * oneMinusT * t * t * p2
                 + t * t * t * p3;
        }

        /// <summary>
        /// 生成 2D 二阶贝塞尔曲线点
        /// </summary>
        public static List<Vector2> GenerateQuadraticBezierPoints2D(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            int pointCount)
        {
            List<Vector2> result = new List<Vector2>();

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateQuadraticBezierPoints2D: 生成点数量至少需要 2 个");
                return result;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                result.Add(GetQuadraticBezierPoint2D(p0, p1, p2, t));
            }

            return result;
        }

        /// <summary>
        /// 生成 2D 三阶贝塞尔曲线点
        /// </summary>
        public static List<Vector2> GenerateCubicBezierPoints2D(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            int pointCount)
        {
            List<Vector2> result = new List<Vector2>();

            if (pointCount < 2)
            {
                Debug.LogWarning("BezierUtil.GenerateCubicBezierPoints2D: 生成点数量至少需要 2 个");
                return result;
            }

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                result.Add(GetCubicBezierPoint2D(p0, p1, p2, p3, t));
            }

            return result;
        }

        #endregion
    }
}