namespace FuzzPhyte.Applications.Analytics
{
    using FuzzPhyte.Utility.Analytics;
    using System.Collections.Generic;
    using FuzzPhyte.Utility.EDU;
    using FuzzPhyte.Utility;
    using FuzzPhyte.XR;
    using UnityEngine;
    using System.Linq;
    using System.Collections;

    public class FP_VocabStatTracker : MonoBehaviour,IFPDontDestroy
    {
        public static FP_VocabStatTracker Instance { get; private set; }
        public bool DontDestroy { get => dontDestroy; set => dontDestroy=value; }
        [SerializeField] protected bool dontDestroy;

        //we really just want to keep tabs on the number of times the audio/label was presented to start
        protected Dictionary<FP_Vocab, FP_StatReporter_Int> vocabMediaInteracted = new();
        //we also want to keep tabs on number of pick ups of this type of vocab
        protected Dictionary<FP_Vocab,FP_StatReporter_Int> vocabInteracted = new();
        [Space]
        [Header("Details")]
        public List<FPSpawner> TrackedSpawners = new List<FPSpawner>();
        protected WaitForEndOfFrame endOfFrameDelay;
        [SerializeField]protected List<FP_Vocab> allVocab = new List<FP_Vocab>();
        [Tooltip("This object needs a stat reporter on it")]
        public GameObject InteractionStatReporterPrefab;
        public GameObject MediaStatReporterPrefab;

        //public Dictionary<FP_Vocab,FP_StatReporter_Int> VocabTrackers { get => vocabTrackers; }
        //public Dictionary<FP_Vocab,FP_StatReporter_Int> VocabInteractions { get => vocabInteractions; }
        //public IReadOnlyDictionary<FP_Vocab, FP_StatReporter_Int> ReturnTrackers() => vocabMediaInteracted;
        public virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (DontDestroy)
            {
                DontDestroyOnLoad(this);
            }
            endOfFrameDelay = new WaitForEndOfFrame();
        }
        #region Setup
        protected void Start()
        {
            //remove duplicates
            allVocab = allVocab.Distinct().ToList();
            vocabMediaInteracted = new Dictionary<FP_Vocab, FP_StatReporter_Int>();
            vocabInteracted = new Dictionary<FP_Vocab, FP_StatReporter_Int>();
            //register for listeners now that we already have in our list
        }
#if UNITY_EDITOR
        [Tooltip("Use this for testing")]
        public List<FPWorldItem> TestWorldItems = new List<FPWorldItem>();
        [ContextMenu("Setup FP World Item Test List Listeners")]
        public void TestSetupFPItems()
        {
            foreach (FPWorldItem v in TestWorldItems)
            {
                RegisterItem(v);
            }
        }
#endif
        protected void OnEnable()
        {
            foreach(var spawner in TrackedSpawners)
            {
                if(spawner is FPXRSpawnStack)
                {
                    var stackSpawn = (FPXRSpawnStack)spawner;
                    stackSpawn.OnSpawnedItem += HandleSpawnedItem;
                }
                else
                {
                    if(spawner is FPXRSpawnPieces)
                    {
                        var pieceSpawn = (FPXRSpawnPieces)spawner;
                        pieceSpawn.OnSpawnedItem += HandleSpawnedItem;
                    }
                }
            }
        }

        protected void OnDisable()
        {
            foreach (var spawner in TrackedSpawners)
            {
                if (spawner is FPXRSpawnStack)
                {
                    var stackSpawn = (FPXRSpawnStack)spawner;
                    stackSpawn.OnSpawnedItem -= HandleSpawnedItem;
                }
                else
                {
                    if (spawner is FPXRSpawnPieces)
                    {
                        var pieceSpawn = (FPXRSpawnPieces)spawner;
                        pieceSpawn.OnSpawnedItem -= HandleSpawnedItem;
                    }
                }
            }
        }
        #endregion
        protected void HandleSpawnedItem(GameObject obj)
        {
            var item = obj.GetComponent<FPWorldItem>();
            if (item != null)
            {
                RegisterItem(item);
            }
        }

        public void RegisterItem(FPWorldItem item)
        {
            void OnGrabbed(FPWorldItem fpItem, XRHandedness hand) => LogVocabInteraction(fpItem, InteractionStatReporterPrefab, vocabInteracted);
            void OnRaySelect(FPWorldItem fpItem, XRHandedness hand) => LogVocabInteraction(fpItem, InteractionStatReporterPrefab, vocabInteracted);
            void OnLabelActivated(FPWorldItem fpItem) => LogVocabInteraction(fpItem, MediaStatReporterPrefab, vocabMediaInteracted);
            
            void OnDestroyed(FPWorldItem fpItem)
            {
                item.ItemGrabbed -= OnGrabbed;
                item.ItemRaySelect -= OnRaySelect;
                item.ItemLabelActivated -= OnLabelActivated;
                item.ItemDestroyed -= OnDestroyed;
            }

            item.ItemGrabbed += OnGrabbed;
            item.ItemRaySelect += OnRaySelect;
            item.ItemLabelActivated += OnLabelActivated;
            item.ItemDestroyed += OnDestroyed;
            //
            // item.ItemGrabbed += (fpItem, hand) => LogVocabInteraction(fpItem, InteractionStatReporterPrefab,vocabInteracted);
            // item.ItemRaySelect += (fpItem, hand) => LogVocabInteraction(fpItem, InteractionStatReporterPrefab,vocabInteracted);
            // item.ItemLabelActivated += (fpItem) => LogVocabInteraction(fpItem, MediaStatReporterPrefab,vocabMediaInteracted);
            // item.ItemDestroyed += (fpItem) => ItemGotDestroyed(fpItem);
        }

        protected virtual void LogVocabInteraction(FPWorldItem item, GameObject prefab,Dictionary<FP_Vocab, FP_StatReporter_Int> storage)
        {
            var vocab = item.DetailedLabelData.VocabData;
           
            if(vocab == null)
            {
                return;
            }
            //build a list of all vocab including support vocab because it's still vocab
            List<FP_Vocab>cachedVocab = new List<FP_Vocab>();
            cachedVocab.Add(vocab);
            var supportVocab = item.DetailedLabelData.SupportVocabData;
            if (supportVocab != null)
            {
                //seen this support vocab?
                for (int i = 0; i < supportVocab.Count; i++)
                {
                    cachedVocab.Add(supportVocab[i].SupportData);
                }
            }
            //seen this vocab?
            for(int j = 0; j < cachedVocab.Count; j++)
            {
                var aVocabCached = cachedVocab[j];
                if (!allVocab.Contains(aVocabCached))
                {
                    allVocab.Add(aVocabCached);
                }
                FP_StatReporter_Int reporter;
                if (!storage.TryGetValue(aVocabCached, out reporter))
                {
                    reporter = CreateTrackerForVocab(prefab,aVocabCached);
                    Debug.Log($"Creating reporter for {aVocabCached.Word}");
                    if(reporter != null)
                    {
                        storage.Add(aVocabCached, reporter);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Debug.Log($"Had the reporter already, {aVocabCached.Word}");
                }
                    StartCoroutine(DelayEndOfFrameSyncReporter(aVocabCached, reporter));
            }
        }
        IEnumerator DelayEndOfFrameSyncReporter(FP_Vocab aVocabCached, FP_StatReporter_Int reporter)
        {
            yield return endOfFrameDelay;
            bool success = false;
            reporter.NewStatData($"Interacted with {aVocabCached.Word}", ref success, 1);
        }
        protected virtual FP_StatReporter_Int CreateTrackerForVocab(GameObject statPrefab,FP_Vocab vocab)
        {
            var trackerGO = Instantiate(statPrefab, this.transform);
            trackerGO.transform.localPosition = Vector3.zero;
            trackerGO.name = $"StatReporter_{vocab.Word}";
            var reporter = trackerGO.GetComponent<FP_StatReporter_Int>();
            if (reporter == null)
            {
                Debug.LogError($"Missing a Stat reporter on the gameobject your spawning!");
                Destroy(trackerGO);
                return null;
            }
            return reporter;
        }
        [ContextMenu("Testing something for the stats")]
        public void EndAllStats()
        {
            var allKeysDictionaryOne = vocabMediaInteracted.Keys.ToList();
            var allKeysDictionaryTwo = vocabInteracted.Keys.ToList();

            for(int i = 0; i < allKeysDictionaryOne.Count; i++)
            {
                var aKey = allKeysDictionaryOne[i];
                vocabMediaInteracted[aKey].EndStatData();
            }
            for (int i = 0; i < allKeysDictionaryTwo.Count; i++)
            {
                var aKey = allKeysDictionaryTwo[i];
                vocabInteracted[aKey].EndStatData();
            }

            //now do the calculation
            //ReturnStatCalculation
            for (int i = 0; i < allKeysDictionaryOne.Count; i++)
            {
                var aKey = allKeysDictionaryOne[i];
                (double valueDictionaryOne, bool success) = vocabMediaInteracted[aKey].ReturnStatCalculation(StatCalculationType.Sum);
                Debug.Log($"Heard/Saw the vocabulary word: {aKey.Word} {valueDictionaryOne}-times");
            }
            for (int i = 0; i < allKeysDictionaryTwo.Count; i++)
            {
                var aKey = allKeysDictionaryTwo[i];
                (double valueDictionaryTwo, bool success) = vocabInteracted[aKey].ReturnStatCalculation(StatCalculationType.Sum);
                Debug.Log($"I Interacted with an object that involved the {aKey.Word} vocabulary term [{valueDictionaryTwo}-times]");
            }
        }
    }
}
