public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("References")]
    public PlayerController playerController;
    public SanitySystem sanitySystem;

    [Header("Save Settings")]
    public string saveFileName = "savegame.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    void Awake()
    {
        // Simple singleton so other scripts can call SaveSystem.Instance.Save()
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Serializable]
    public class SaveData
    {
        public float posX, posY, posZ;
        public float rotY; // yaw rotation (horizontal facing)
        public float currentStamina;
        public float currentSanity;
        public string sceneName;
        public string savedAtUtc;
    }

    public void Save()
    {
        if (playerController == null)
        {
            Debug.LogWarning("SaveSystem: PlayerController reference missing, cannot save.");
            return;
        }

        Transform playerTransform = playerController.transform;

        SaveData data = new SaveData
        {
            posX = playerTransform.position.x,
            posY = playerTransform.position.y,
            posZ = playerTransform.position.z,
            rotY = playerTransform.eulerAngles.y,
            currentStamina = playerController.GetStaminaPercent() * playerController.maxStamina,
            currentSanity = sanitySystem != null ? sanitySystem.GetSanityPercent() * sanitySystem.maxSanity : -1f,
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Game saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: Failed to save game. {e.Message}");
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public void Load()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("SaveSystem: No save file found.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            ApplyLoadedData(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: Failed to load game. {e.Message}");
        }
    }

    private void ApplyLoadedData(SaveData data)
    {
        // If the save is from a different scene, you'd normally load that scene first
        // and re-apply this data in a callback after the scene finishes loading.
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (data.sceneName != currentScene)
        {
            Debug.LogWarning($"SaveSystem: Save was from scene '{data.sceneName}', " +
                              $"currently in '{currentScene}'. Load that scene before applying position data.");
        }

        if (playerController == null)
        {
            Debug.LogWarning("SaveSystem: PlayerController reference missing, cannot apply load data.");
            return;
        }

        // CharacterController must be disabled briefly to teleport safely
        CharacterController cc = playerController.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerController.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        playerController.transform.eulerAngles = new Vector3(0f, data.rotY, 0f);

        if (cc != null) cc.enabled = true;

        playerController.SetStamina(data.currentStamina);

        if (sanitySystem != null && data.currentSanity >= 0f)
            sanitySystem.SetSanity(data.currentSanity);

        Debug.Log($"Game loaded from {SavePath} (saved at {data.savedAtUtc})");
    }

    public void DeleteSave()
    {
        if (HasSaveFile())
        {
            File.Delete(SavePath);
            Debug.Log("Save file deleted.");
        }
    }
}