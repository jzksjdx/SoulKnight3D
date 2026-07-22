using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class MerchantStockItem : InteractiveItem
    {
        public event Action Purchased;

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
        private float _potionYOffset;

        public static MerchantStockItem Create(Transform stockPoint, GameObject itemPrefab, int price,
            GameObject priceLabelPrefab, float potionYOffset)
        {
            GameObject container = new GameObject($"MerchantStock_{itemPrefab.name}");
            container.layer = itemPrefab.layer;
            container.transform.SetParent(stockPoint, false);
            container.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            BoxCollider interactionCollider = container.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 0f, 0f);
            interactionCollider.size = new Vector3(0.5f, 0.5f, 0.5f);

            MerchantStockItem stockItem = container.AddComponent<MerchantStockItem>();
            stockItem.InteractCollider = interactionCollider;
            stockItem._price = Mathf.Max(1, price);
            stockItem._potionYOffset = Mathf.Max(0f, potionYOffset);
            stockItem.CreateLabel(priceLabelPrefab);
            stockItem.SetProduct(Instantiate(itemPrefab, container.transform));
            return stockItem;
        }

        protected override void Start()
        {
            base.Start();
            RefreshLabel();
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
            if (_productInteraction is Potion potion)
            {
                potion.SetInteractable(true);
                potion.Interact();
            }
            else
            {
                ReleaseProduct();
            }
            Purchased?.Invoke();
            AudioKit.PlaySound("fx_buy");
            Destroy(gameObject);
        }

        private void SetProduct(GameObject product)
        {
            _product = product;
            _product.name = product.name.Replace("(Clone)", string.Empty).Trim();
            _product.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _productInteraction = _product.GetComponent<InteractiveItem>();
            if (_productInteraction is Potion)
            {
                _product.transform.localPosition = Vector3.up * _potionYOffset;
            }

            RefreshLabel();
            SnapLabelToProductLabel();

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
            _productInteraction?.SetInteractable(false);

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

        private void CreateLabel(GameObject priceLabelPrefab)
        {
            GameObject labelObject = priceLabelPrefab != null
                ? Instantiate(priceLabelPrefab, transform)
                : new GameObject("PriceLabel");
            labelObject.name = "PriceLabel";
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.3f, 0f);

            _priceLabel = labelObject.GetComponent<MerchantPriceLabel>();
            if (_priceLabel == null)
            {
                _priceLabel = labelObject.AddComponent<MerchantPriceLabel>();
            }

            Label = _priceLabel;
            RefreshLabel();
            labelObject.SetActive(false);
        }

        private void RefreshLabel()
        {
            if (_priceLabel == null) { return; }

            if (_languageSystem == null)
            {
                _languageSystem = this.GetSystem<LanguageSystem>();
            }

            string displayName = _product != null ? _product.name : string.Empty;
            WeaponData.WeaponRarity rarity = WeaponData.WeaponRarity.White;
            if (_productInteraction is PickupWeapon pickupWeapon
                && pickupWeapon.WeaponData != null)
            {
                displayName = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese
                    ? pickupWeapon.WeaponData.NameCN
                    : pickupWeapon.WeaponData.Name;
                rarity = pickupWeapon.WeaponData.Rarity;
            }
            else if (_productInteraction is Potion potion)
            {
                displayName = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese
                    ? potion.DisplayNameCN
                    : potion.DisplayName;
            }

            _priceLabel.Initialize(_price, displayName, rarity);
        }

        private void SnapLabelToProductLabel()
        {
            if (_priceLabel == null || _productInteraction == null || _productInteraction.Label == null)
            {
                return;
            }

            _priceLabel.transform.position = _productInteraction.Label.transform.position;
        }
    }

    internal sealed class MerchantPriceLabel : InteractLabel
    {
        private static readonly Color AffordableColor = new Color(1f, 0.78f, 0.12f);
        private static readonly Color UnaffordableColor = new Color(1f, 0.3f, 0.24f);

        private TextMesh _priceText;
        private TextMesh _nameText;
        private Camera _mainCamera;
        private int _price;
        private float _flashTimeout;

        public void Initialize(int price, string itemName, WeaponData.WeaponRarity rarity)
        {
            _price = price;
            CacheTextMeshes();

            if (_priceText != null)
            {
                _priceText.text = price.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = itemName;
                _nameText.color = GetLabelColor(rarity);
                LabelText = _nameText;
            }
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

            if (_priceText == null)
            {
                CacheTextMeshes();
            }
            if (_priceText == null) { return; }

            if (_flashTimeout > 0f)
            {
                _flashTimeout -= Time.deltaTime;
                _priceText.color = Color.white;
                return;
            }

            PlayerController player = PlayerController.Instance;
            bool canAfford = player != null && player.PlayerStats.Coins.Value >= _price;
            _priceText.color = canAfford ? AffordableColor : UnaffordableColor;
        }

        public void FlashInsufficientFunds()
        {
            _flashTimeout = 0.25f;
        }

        private void CacheTextMeshes()
        {
            TextMesh[] texts = GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == "PriceText")
                {
                    _priceText = texts[i];
                }
                else if (texts[i].name == "NameText")
                {
                    _nameText = texts[i];
                }
            }

            if (_priceText == null && texts.Length > 0)
            {
                _priceText = texts[0];
            }
            if (_nameText == null && texts.Length > 1)
            {
                _nameText = texts[1];
            }
        }

        private static Color GetLabelColor(WeaponData.WeaponRarity rarity)
        {
            switch (rarity)
            {
                case WeaponData.WeaponRarity.White:
                    return Color.white;
                case WeaponData.WeaponRarity.Green:
                    return new Color(61f / 255, 226f / 255, 90f / 255);
                case WeaponData.WeaponRarity.Blue:
                    return new Color(21f / 255, 165f / 255, 251f / 255);
                case WeaponData.WeaponRarity.Purple:
                    return new Color(191f / 255, 62f / 255, 202f / 255);
                case WeaponData.WeaponRarity.Orange:
                    return new Color(248f / 255, 138f / 255, 29f / 255);
                case WeaponData.WeaponRarity.Red:
                    return new Color(226f / 255, 27f / 255, 27f / 255);
                case WeaponData.WeaponRarity.Magenta:
                    return new Color(255f / 255f, 67f / 255f, 214f / 255f);
                default:
                    return Color.white;
            }
        }
    }
}
