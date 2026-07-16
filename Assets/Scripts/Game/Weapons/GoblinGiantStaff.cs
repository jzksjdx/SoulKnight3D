using UnityEngine;

namespace SoulKnight3D
{
    public class GoblinGiantStaff : Gun
    {
        [Header("Volley Pattern")]
        [SerializeField, Min(1)] private int _rowCount = 5;
        [SerializeField, Min(1)] private int _bulletsPerRow = 3;
        [SerializeField, Min(0f)] private float _horizontalArcAngle = 45f;
        [Tooltip("Extra horizontal separation between rows. The total arc remains unchanged.")]
        [SerializeField, Min(0f)] private float _rowGapAngle = 3f;

        [Header("Aim Heights")]
        [SerializeField] private float _oddRowAimHeight = 0.65f;
        [SerializeField] private float _evenRowAimHeight = 0.2f;

        public override void ShootAtTarget(Transform target, Vector3 aimPosition)
        {
            if (target == null || shootPoint == null)
            {
                return;
            }

            int rowCount = Mathf.Max(1, _rowCount);
            int bulletsPerRow = Mathf.Max(1, _bulletsPerRow);
            int totalBulletCount = rowCount * bulletsPerRow;
            int rowBoundaryCount = rowCount - 1;
            float rowGapAngle = rowBoundaryCount > 0
                ? Mathf.Clamp(_rowGapAngle, 0f, _horizontalArcAngle / rowBoundaryCount)
                : 0f;
            float angleStep = totalBulletCount > 1
                ? (_horizontalArcAngle - rowGapAngle * rowBoundaryCount) / (totalBulletCount - 1)
                : 0f;
            float firstAngle = -_horizontalArcAngle * 0.5f;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                float aimHeight = rowIndex % 2 == 0 ? _oddRowAimHeight : _evenRowAimHeight;
                Vector3 rowTarget = target.position + Vector3.up * aimHeight;
                Vector3 rowDirection = rowTarget - shootPoint.position;
                if (rowDirection.sqrMagnitude <= 0.0001f)
                {
                    rowDirection = target.forward;
                }
                rowDirection.Normalize();

                for (int bulletIndex = 0; bulletIndex < bulletsPerRow; bulletIndex++)
                {
                    int volleyIndex = rowIndex * bulletsPerRow + bulletIndex;
                    float horizontalAngle = firstAngle + angleStep * volleyIndex + rowGapAngle * rowIndex;
                    Vector3 bulletDirection = Quaternion.AngleAxis(horizontalAngle, Vector3.up) * rowDirection;

                    Bullet bullet = SpawnBulletFromPool(shootPoint.position);
                    bullet.SelfRigidbody.velocity = bulletDirection * BulletSpeed;
                    bullet.transform.rotation = Quaternion.LookRotation(bulletDirection);
                }
            }

            ShootFeedback?.PlayFeedbacks();
        }
    }
}
