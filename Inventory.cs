using UnityEngine;
using System.Collections.Generic;
public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public string itemName;
        public string description;
        public Sprite icon;
        public ItemType type;
    }

    public enum ItemType { Key, Battery, MedKit, Lore, Misc }

    private List<Item> items = new List<Item>();

    public bool AddItem(Item item)
    {
        if (items.Count >= 8)
        {
            Debug.Log("[Inventory] Full — cannot add: " + item.itemName);
            return false;
        }
        items.Add(item);
        Debug.Log("[Inventory] Picked up: " + item.itemName);

        // Auto-use batteries
        if (item.type == ItemType.Battery)
        {
            FlashlightSystem fl = GetComponent<FlashlightSystem>();
            if (fl != null)
            {
                fl.AddBattery(40f);
                items.Remove(item);
            }
        }

        // Auto-use medkits to restore sanity
        if (item.type == ItemType.MedKit)
        {
            SanitySystem sanity = GetComponent<SanitySystem>();
            if (sanity != null)
            {
                sanity.RestoreSanity(25f);
                items.Remove(item);
            }
        }

        InventoryUI.Instance?.Refresh(items);
        return true;
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(i => i.itemName.ToLower() == itemName.ToLower());
    }

    public bool RemoveItem(string itemName)
    {
        Item found = items.Find(i => i.itemName.ToLower() == itemName.ToLower());
        if (found != null)
        {
            items.Remove(found);
            InventoryUI.Instance?.Refresh(items);
            return true;
        }
        return false;
    }

    public List<Item> GetItems() => items;
}

public class PickupItem : Interactable
{
    public PlayerInventory.Item item;

    void Start()
    {
        oneTimeUse = true;
        promptText = $"Press E to pick up {item?.itemName}";
    }

    protected override void Interact()
    {
        PlayerInventory inv = FindObjectOfType<PlayerInventory>();
        if (inv != null && inv.AddItem(item))
            gameObject.SetActive(false);
    }
}

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    public GameObject panel;
    public UnityEngine.UI.Text itemListLabel;

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool open = panel != null && panel.activeSelf;
            if (panel != null) panel.SetActive(!open);
            Time.timeScale = open ? 1f : 0f;
        }
    }

    public void Refresh(List<PlayerInventory.Item> items)
    {
        if (itemListLabel == null) return;
        if (items.Count == 0)
        {
            itemListLabel.text = "(empty)";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var item in items)
            sb.AppendLine($"• {item.itemName}  [{item.type}]");
        itemListLabel.text = sb.ToString();
    }
}
