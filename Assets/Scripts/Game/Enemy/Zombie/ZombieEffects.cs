using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class ZombieEffects : MonoBehaviour
    {
        public AudioClip[] HitSounds;
        public AudioClip[] GroanSounds;
        public AudioClip[] AttackSounds;
        public AudioClip[] LimpPopSounds;
        public AudioClip[] DieSounds;

        // feedbacks
        public MMF_Player SoundFeedbacks;
        private MMF_MMSoundManagerSound _sound;

        // transforms
        public Transform LeftArmTransform;
        public Transform LeftForeArmTransform;
        public Transform HeadTransform;
        public GameObject BoneMesh;
        public GameObject BrokenArm;
        public GameObject Head;
        private Vector3 _brokenArmPosition = new Vector3(-0.118000001f, 0.374000013f, 0.064000003f);
        private Vector3 _popArmScale = new Vector3(1.85739994f, 0.541021466f, 1.85739994f);
        private Vector3 _HeadPosition = new Vector3(0.0037f, 0.542299986f, 0.0705000013f);

        private bool _isArmPoped = false;

        // timeouts
        private float _groanTimeout;
        private float _groanTimeoutDelta;

        private void Start()
        {
            _sound = SoundFeedbacks.GetFeedbackOfType<MMF_MMSoundManagerSound>();
            ResetGroanTime();
        }

        private void Update()
        {
            // play groan sound
            if (_groanTimeoutDelta >= 0)
            {
                _groanTimeoutDelta -= Time.deltaTime;
                if (_groanTimeoutDelta <= 0)
                {
                    PlayGroanSound();
                    ResetGroanTime();
                }
            }

        }

        public void ResetEffects()
        {
            _isArmPoped = false;
            BoneMesh.Hide();
            BrokenArm.Hide();
            Head.Hide();
            BrokenArm.transform.position = _brokenArmPosition;
            Head.transform.position = _HeadPosition;
            LeftForeArmTransform.localScale = Vector3.one;
            LeftArmTransform.localScale = Vector3.one;
            HeadTransform.localScale = Vector3.one;
            ResetGroanTime();
        }


        private void ResetGroanTime()
        {
            _groanTimeout = Random.Range(7f, 20f);
            _groanTimeoutDelta = _groanTimeout;
        }

        // pop limbs
        public void PopLimb()
        {
            if (_isArmPoped) { return; }
            _isArmPoped = true;
            LeftForeArmTransform.localScale = Vector3.zero;
            LeftArmTransform.localScale = _popArmScale;
            BoneMesh.Show();
            BrokenArm.Show();
            PlayLimpPopSound();

            // arm simulation
            BrokenArm.transform.parent = null;
            Rigidbody rb = BrokenArm.GetComponent<Rigidbody>();
            Vector3 randomForce = new Vector3(Random.Range(-0.2f, 0.2f), 0.2f, Random.Range(-0.2f, 0.2f));
            rb.AddForce(randomForce, ForceMode.Impulse);
            ActionKit.Delay(2f, () =>
            {
                BrokenArm.Hide();
                BrokenArm.transform.parent = transform;
            }).Start(this);
        }

        public void PopHead()
        {
            HeadTransform.localScale = Vector3.zero;
            PlayLimpPopSound();
            Head.Show();

            // head simulation
            Head.transform.parent = null;
            Rigidbody rb = Head.GetComponent<Rigidbody>();
            Vector3 randomForce = new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, Random.Range(-0.3f, 0.3f));
            rb.AddForce(randomForce, ForceMode.Impulse);
            ActionKit.Delay(2f, () =>
            {
                Head.Hide();
                Head.transform.parent = transform;
            }).Start(this);
        }

        // play feedback sounds
        public void PlayHitSound()
        {
            _sound.RandomSfx = HitSounds;
            SoundFeedbacks?.PlayFeedbacks();
        }

        public void PlayGroanSound()
        {
            _sound.RandomSfx = GroanSounds;
            SoundFeedbacks?.PlayFeedbacks();
        }

        public void PlayAttackSound()
        {
            _sound.RandomSfx = AttackSounds;
            SoundFeedbacks?.PlayFeedbacks();
        }

        public void PlayLimpPopSound()
        {
            _sound.RandomSfx = LimpPopSounds;
            SoundFeedbacks?.PlayFeedbacks();
        }


        public void PlayDieound()
        {
            _sound.RandomSfx = DieSounds;
            SoundFeedbacks?.PlayFeedbacks();
        }
    }

}
