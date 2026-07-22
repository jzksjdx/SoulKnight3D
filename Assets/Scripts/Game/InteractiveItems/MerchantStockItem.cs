using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class MerchantStockItem : InteractiveItem
    {
        private struct ColliderState
        {
            public Collider Collider;
            public bool WasEnabled;
        }

        private readonly List<ColliderState> _colliderStates = new List<ColliderState>();
        private GameObject _product;
        private InteractiveItem _productInteraction;
        private Rigidbody _productRigidbody;
        private bool _wasKinematic;
        private bool _usedGravity;
        private int _price;
        private bool _purchased;
        private MerchantPriceLabel _priceLabel;

        public static MerchantStockItem Create(Transform stockPoint, GameObject itemPrefab, int price)
        {
            GameObject container = new GameObject($"MerchantStock_{itemPrefab.name}");
            container.layer = itemPrefab.layer;
            container.transform.SetParent(stockPoint, false);
            container.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            BoxCollider interactionCollider = container.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 0.0f, 0f);
            interactionCollider.size = new Vector3(0.5f, 0.5f, 0.5f);

            MerchantStockItem stockItem = container.AddComponent<MerchantStockItem>();
            stockItem.InteractCollider = interactionCollider;
            stockItem._price = Mathf.Max(1, price);
            stockItem.CreateLabels();
            stockItem.SetProduct(Instantiate(itemPrefab, container.transform));
            return stockItem;
        }

        protected override void Start()
        {
            base.Start();
            if (Label != null)
            {
                string labelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese
                    ? "购买"
                    : "Buy";
                Label.SetLabelText(labelText, WeaponData.WeaponRarity.White);
            }
        }

        public override void Interact()
        {
            if (!IsInteractable || _purchased) { return; }

            PlayerController player = PlayerController.Instance;
            if (player == null || !player.PlayerStats.TrySpendCoins(_price))
            {
                _priceLabel?.FlashInsufficientFunds();
                AudioKit.PlaySound("fx_btn");
                return;
            }

            _purchased = true;
            SetInteractable(false);
            ReleaseProduct();
            AudioKit.PlaySound("fx_buy");
            Destroy(gameObject);
        }

        private void SetProduct(GameObject product)
        {
            _product = product;
            _product.name = product.name.Replace("(Clone)", string.Empty).Trim();
            _product.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _productInteraction = _product.GetComponent<InteractiveItem>();
            _productInteraction?.SetInteractable(false);

            Collider[] colliders = _product.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                _colliderStates.Add(new ColliderState
                {
                    Collider = colliders[i],
                    WasEnabled = colliders[i].enabled
                });
                colliders[i].enabled = false;
            }

            _productRigidbody = _product.GetComponent<Rigidbody>();
            if (_productRigidbody != null)
            {
                _wasKinematic = _productRigidbody.isKinematic;
                _usedGravity = _productRigidbody.useGravity;
                _productRigidbody.velocity = Vector3.zero;
                _productRigidbody.angularVelocity = Vector3.zero;
                _productRigidbody.isKinematic = true;
                _productRigidbody.useGravity = false;
            }
        }

        private void ReleaseProduct()
        {
            if (_product == null) { return; }

            Transform stockPoint = transform.parent;
            _product.transform.SetParent(stockPoint, true);
            _product.transform.position += Vector3.up * 0.03f;

            _productInteraction?.SetInteractable(true);
            for (int i = 0; i < _colliderStates.Count; i++)
            {
                ColliderState state = _colliderStates[i];
                if (state.Collider != null)
                {
                    state.Collider.enabled = state.WasEnabled;
                }
            }

            if (_productRigidbody != null)
            {
                _productRigidbody.isKinematic = _wasKinematic;
                _productRigidbody.useGravity = _usedGravity;
            }
        }

        private void CreateLabels()
        {
            GameObject priceObject = CreateTextObject("PriceLabel", _price.ToString(),
                new Vector3(0f, 0.7f, 0f), 0.013f);
            _priceLabel = priceObject.AddComponent<MerchantPriceLabel>();
            _priceLabel.Initialize(priceObject.GetComponent<TextMesh>(), _price);

            GameObject promptObject = CreateTextObject("InteractLabel", "Buy",
                new Vector3(0f, 0.94f, 0f), 0.011f);
            InteractLabel prompt = promptObject.AddComponent<InteractLabel>();
            prompt.LabelText = promptObject.GetComponent<TextMesh>();
            Label = prompt;
            promptObject.SetActive(false);
        }

        private GameObject CreateTextObject(string objectName, string text, Vector3 localPosition,
            float characterSize)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = localPosition;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.color = new Color(1f, 0.78f, 0.12f);
            return textObject;
        }
    }

    internal sealed class MerchantPriceLabel : MonoBehaviour
    {
        private static readonly Color AffordableColor = new Color(1f, 0.78f, 0.12f);
        private static readonly Color UnaffordableColor = new Color(1f, 0.3f, 0.24f);

        private TextMesh _textMesh;
        private Camera _mainCamera;
        private int _price;
        private float _flashTimeout;

        public void Initialize(TextMesh textMesh, int price)
        {
            _textMesh = textMesh;
            _price = price;
        }

        private void Update()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            if (_mainCamera != null)
            {
                transform.LookAt(_mainCamera.transform);
                transform.Rotate(0f, 180f, 0f);
            }

            if (_textMesh == null) { return; }
            if (_flashTimeout > 0f)
            {
                _flashTimeout -= Time.deltaTime;
                _textMesh.color = Color.white;
                return;
            }

            PlayerController player = PlayerController.Instance;
            bool canAfford = player != null && player.PlayerStats.Coins.Value >= _price;
            _textMesh.color = canAfford ? AffordableColor : UnaffordableColor;
        }

        public void FlashInsufficientFunds()
        {
            _flashTimeout = 0.25f;
        }
    }
}
