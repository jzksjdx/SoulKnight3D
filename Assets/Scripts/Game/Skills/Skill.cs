using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class Skill : MonoBehaviour
    {
        public BindableProperty<float> SkillCdNormalized = new BindableProperty<float>();


        // timeout deltatime
        protected float _skillCooldownDelta;
        [SerializeField] protected float _skillCooldown = 1f;
        protected float _skillDurationDelta;
        [SerializeField] protected float _skillDuration = 5f;

        public bool IsUsingSkill = false;

        protected virtual void Start()
        {
            _skillCooldownDelta = _skillCooldown;

            PlayerInputs.Instance.OnSkillPerformed.Register(() =>
            {
                UseSkill();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }


        protected virtual void Update()
        {
            if (!IsUsingSkill && _skillCooldownDelta >= 0) // cooling down
            {
                _skillCooldownDelta -= Time.deltaTime;
                SkillCdNormalized.Value = 1 - _skillCooldownDelta / _skillCooldown;
            }
            else if (IsUsingSkill)
            {
                UsingSkillOnUpdate();
            }
        }

        protected virtual void UsingSkillOnUpdate()
        {
            if (_skillDurationDelta >= 0)
            {
                _skillDurationDelta -= Time.deltaTime;
                SkillCdNormalized.Value = _skillDurationDelta / _skillDuration;
            }
            else
            {
                // skill ends
                HandleSkillEnd();
            }
        }

        protected virtual void HandleSkillEnd()
        {
            IsUsingSkill = false;
            _skillDurationDelta = 0f;
            _skillCooldownDelta = _skillCooldown;
            SkillCdNormalized.Value = 0f;
        }

        public virtual bool UseSkill()
        {
            if (_skillCooldownDelta > 0f || IsUsingSkill) { return false; }
            _skillDurationDelta = _skillDuration;
            IsUsingSkill = true;
            return true;
        }

        public virtual void CancelForLevelTransition()
        {
            if (IsUsingSkill)
            {
                HandleSkillEnd();
                return;
            }

            _skillDurationDelta = 0f;
        }
    }

}
