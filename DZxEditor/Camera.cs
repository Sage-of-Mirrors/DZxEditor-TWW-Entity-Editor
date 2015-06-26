using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace DZxEditor
{
    public class Camera
    {
        public Matrix4 ViewMatrix
        {
            get { return Matrix4.LookAt(eye, target, Vector3.UnitY); }
        }

        public Vector3 EyePos
        {
            get { return eye; }
            set { eye = value; }
        }

        Vector3 eye = new Vector3(0, 0, 1);

        public Vector3 TargetPos
        {
            get { return target; }
            set { target = value; }
        }

        Vector3 target = Vector3.Zero;

        Transform Trans = new Transform();

        public void Update()
        {
            float MoveSpeed = 50f;

            Vector3 moveDir = Vector3.Zero;

            if (Input.GetKey(Keys.W))
            {
                moveDir += Vector3.UnitZ;
            }

            if (Input.GetKey(Keys.S))
            {
                moveDir -= Vector3.UnitZ;
            }

            if (Input.GetKey(Keys.A))
            {
                moveDir += Vector3.UnitX;
            }

            if (Input.GetKey(Keys.D))
            {
                moveDir -= Vector3.UnitX;
            }

            if (Input.GetMouseButton(0))
            {
                Rotate(Input.MouseDelta.X, Input.MouseDelta.Y);
            }

            float moveSpeed = Input.GetKey(Keys.Space) ? MoveSpeed * 10f : MoveSpeed;

            // Normalize the move direction
            moveDir.NormalizeFast();

            // Make it relative to the current rotation.
            moveDir = Trans.Rotation.Multiply(moveDir);

            Trans.Position += Vector3.Multiply(moveDir, moveSpeed);

            eye = Trans.Position;

            target = Trans.Position + Trans.Forward;
        }

        private void Rotate(float x, float y)
        {
            Trans.Rotate(Vector3.UnitY, -x);
            Trans.Rotate(Trans.Right, y);

            // Clamp them from looking over the top point.
            Vector3 up = Vector3.Cross(Trans.Forward, Trans.Right);
            if (Vector3.Dot(up, Vector3.UnitY) < 0.01f)
            {
                Trans.Rotate(Trans.Right, -y);
            }
        }
    }

    public class Transform
    {
        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public Vector3 Right
        {
            get { return Rotation.Multiply(Vector3.UnitX); }
        }

        public Vector3 Forward
        {
            get { return Rotation.Multiply(Vector3.UnitZ); }
        }

        public Vector3 Up
        {
            get { return Rotation.Multiply(Vector3.UnitY); }
        }

        public void LookAt(Vector3 worldPosition)
        {
            Rotation = Quaternion.FromAxisAngle(Vector3.Normalize((Position - worldPosition)), 0f);
        }

        public void Rotate(Vector3 axis, float angleInDegrees)
        {
            Quaternion rotQuat = Quaternion.FromAxisAngle(axis, MathHelper.DegreesToRadians(angleInDegrees));
            Rotation = rotQuat * Rotation;
        }

        public void Translate(Vector3 amount)
        {
            Position += amount;
        }
    }
}
