using UnityEngine;
using UnityEngine.Purchasing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour, IStoreListener
{

    
    private static IStoreController storeController;
    private static IExtensionProvider extensionProvider;

    // Уникальные идентификаторы продуктов
    public const string RUBY_PRODUCT_ID_SMALL = "com.defaultcompany.rubysmall";
    public const string RUBY_PRODUCT_ID_MIDDLE = "com.defaultcompany.rubymiddle";
    public const string RUBY_PRODUCT_ID_BIG = "com.defaultcompany.rubybig";
    public const string RUBY_PRODUCT_ID_VERYBIG = "com.defaultcompany.rubyverybig";
    public const string RUBY_PRODUCT_ID_ELIXIRSMALL = "com.defaultcompany.elixirsmall";
    public const string RUBY_PRODUCT_ID_ELIXIRMIDDLE = "com.defaultcompany.elixirmiddle";
    public const string RUBY_PRODUCT_ID_CATMONEYSMALL = "com.defaultcompany.catmoneysmall";
    [SerializeField] private TMPro.TMP_Text rubysmallPriceText;
    [SerializeField] private TMPro.TMP_Text rubymiddlePriceText;
    [SerializeField] private TMPro.TMP_Text rubybigPriceText;
    [SerializeField] private TMPro.TMP_Text rubyverybigPriceText;
    [SerializeField] private TMPro.TMP_Text elixirsmallPriceText;
    [SerializeField] private TMPro.TMP_Text elixirmiddlePriceText;
    [SerializeField] private TMPro.TMP_Text catmoneysmallPriceText;



//    public const string ELIXIR_PRODUCT_ID = "com.mycompany.elixirsmall";
//    public const string CATMONEY_PRODUCT_ID = "com.mycompany.catmoneysmall";

    private void Start()
    {
//        DontDestroyOnLoad(gameObject);

        if (storeController != null && storeController.products != null)
        {
            Debug.Log("🔄 Обновляем список товаров...");
            var products = storeController.products.all;
            foreach (var product in products)
            {
                Debug.Log($"✅ Товар в системе: {product.definition.id}");
            }
        }

        if (storeController == null || storeController.products.all.Length == 0)
        {
            InitializePurchasing();
        }
        else
        {
            Debug.Log("♻ IAP уже инициализирован, но обновляем данные");
            RefreshIAPProducts();
        }

    }

    private void RefreshIAPProducts()
    {
        if (storeController != null)
        {
            Debug.Log("🔄 Обновляем список продуктов IAP...");
            
            foreach (var product in storeController.products.all)
            {
                if (product != null && product.availableToPurchase)
                {
                    //Debug.Log($"✅ Доступен продукт: {product.definition.id} | Цена: {product.metadata.localizedPriceString}");
                }
                else
                {
                    Debug.LogWarning($"⚠ Продукт не доступен для покупки: {product.definition.id}");
                }
            }
        }
        else
        {
            Debug.LogError("❌ storeController == null, не удалось обновить продукты IAP!");
        }
    }






    private void OnEnable()
    {
        // Проверяем, инициализирован ли магазин
        if (storeController != null)
        {
            UpdatePrices();
        }
        else
        {
            InitializePurchasing();
        }
    }



    // Инициализация покупок
    public void InitializePurchasing()
    {
        if (storeController != null)
        {
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Регистрация продуктов
        builder.AddProduct(RUBY_PRODUCT_ID_SMALL, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_MIDDLE, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_BIG, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_VERYBIG, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_ELIXIRSMALL, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_ELIXIRMIDDLE, ProductType.Consumable);
        builder.AddProduct(RUBY_PRODUCT_ID_CATMONEYSMALL, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }

    // Обработчик успешной инициализации
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;

        Debug.Log("Unity IAP инициализирован.");

        // Обновляем цены на кнопках
        UpdatePrices();
    }

    private void UpdatePrices()
    {
        rubysmallPriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_SMALL);
        rubymiddlePriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_MIDDLE);
        rubybigPriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_BIG);
        rubyverybigPriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_VERYBIG);
        elixirsmallPriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_ELIXIRSMALL);
        elixirmiddlePriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_ELIXIRMIDDLE);
        catmoneysmallPriceText.text = GetLocalizedPrice(ShopManager.RUBY_PRODUCT_ID_CATMONEYSMALL);
    }

    // Обработчик ошибки инициализации
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"Ошибка инициализации IAP: {error}");
    }

    // Покупка продукта
    public void BuyProduct(string productId)
    {
        if (storeController == null)
        {
            Debug.LogError("IAP не инициализирован.");
            return;
        }

        storeController.InitiatePurchase(productId);
    }

    // Обработчик успешной покупки
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_SMALL)
        {
            Debug.Log("Покупка маленьких рубинов выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_RUBY", 70);
            //RubyManagerShop.Instance.SubtractRuby(5); // Вычесть 5 рубинов
            // Добавьте рубины игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_MIDDLE)
        {
            Debug.Log("Покупка средних рубинов выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_RUBY", 150);
            // Добавьте эликсиры игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_BIG)
        {
            Debug.Log("Покупка больших рубинов выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_RUBY", 300); // Добавить 300 рубинов
            // Добавьте Catmoney игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_VERYBIG)
        {
            Debug.Log("Покупка очень больших рубинов выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_RUBY", 900); // Добавить 900 рубинов
            // Добавьте Catmoney игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_ELIXIRSMALL)
        {   
            Debug.Log("Покупка маленьких эликсиров выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 5); // Добавить 5 эликсиров
            // Добавьте Catmoney игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_ELIXIRMIDDLE)
        {
            Debug.Log("Покупка средних эликсиров выполнена.");
            GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 15); // Добавить 15 эликсиров
            // Добавьте Catmoney игроку
        }
        else if (args.purchasedProduct.definition.id == RUBY_PRODUCT_ID_CATMONEYSMALL)
        {
            Debug.Log("Покупка маленьких котомани выполнена.");
            GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 3); // Добавить 3 котомани
            // Добавьте Catmoney игроку
        }
        else
        {
            Debug.LogWarning($"Неизвестный продукт: {args.purchasedProduct.definition.id}");
        }

        return PurchaseProcessingResult.Complete;
    }

    // Обработчик ошибки покупки
    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogError($"Ошибка покупки {product.definition.id}: {reason}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        throw new System.NotImplementedException();
    }


    public string GetLocalizedPrice(string productId)
    {
        if (storeController != null)
        {
            Product product = storeController.products.WithID(productId);
            if (product != null)
            {
                // Возвращаем строку вида "99 RUB"
                return $"{product.metadata.localizedPriceString} {product.metadata.isoCurrencyCode}";
            }
        }
        return "N/A"; // Если данные недоступны
    }

}