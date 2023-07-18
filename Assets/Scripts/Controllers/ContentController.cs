using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContentController : MonoBehaviour
{
    static public ContentController me;

    [SerializeField] private Transform feedEntriesTransform;
    [SerializeField] private GameObject FeedEntryPrefab;
    [SerializeField] private RenderTexture renderTexture;

    private List<string> presetFeedEntryNames = new List<string>();

    private int lastPreviewingDirection = 0;
    private string previewingName = "";

    private string currentViewingLayer = "";

    void Start()
    {
        me = this;

        // START WITH A FEW PRESET FEED ENTRIES
        presetFeedEntryNames.Add("ContentScene1");
        presetFeedEntryNames.Add("ContentScene2");
        presetFeedEntryNames.Add("ContentScene3");

        // CLEARING THIS STATIC CLASS OURSELVES SINCE DOMAIN RELOADING IS DISABLED (FOR FASTER PLAY TIMES)
        Events.Clear();

        Events.scrollFeedStart += (feedNum, scrollDirection) => { ScrollFeedStart(feedNum, scrollDirection); };
        Events.scrollFeedEnd += (feedNum, scrollDirection) => { ScrollFeedEnd(feedNum, scrollDirection); };
        Events.draggingFeed += (feedNum, feedY) => { DraggingFeed(feedNum, feedY); };

        var firstEntryName = presetFeedEntryNames[0];
        StartCoroutine(LoadNewScene(firstEntryName));
    }

    private void ScrollFeedStart(int feedNum, int scrollDirection)
    {
        // WHEN POINTER RELEASES AND SCROLL START, LOAD NEW ENTRY SCENE IF NOT ALREADY PREVIEWING

        if (scrollDirection == 0)
            return;

        var content = GetContentOfScene(previewingName);
        content.Play();

        if (previewingName == "")
        {
            var sceneName = presetFeedEntryNames[feedNum];
            StartCoroutine(LoadNewScene(sceneName, scrollDirection));
        }

        previewingName = "";
        lastPreviewingDirection = 0;
    }

    private void ScrollFeedEnd(int feedNum, int scrollDirection)
    {
        // ONCE SCROLL COMPLETE, UNLOAD AND RESET A BUNCH OF STUFF

        var sceneName = presetFeedEntryNames[feedNum];
        var content = GetContentOfScene(sceneName);
        content.Play();

        if (feedNum == 0 && scrollDirection == 0 && lastPreviewingDirection == -1)
            return;

        if (previewingName != "")
        {
            StartCoroutine(UnloadSceneAsync(previewingName, -lastPreviewingDirection));
            previewingName = "";
            lastPreviewingDirection = 0;
        }
        else
        {
            if (scrollDirection == 0)
                return;

            var previousSceneName = presetFeedEntryNames[feedNum - scrollDirection];
            StartCoroutine(UnloadSceneAsync(previousSceneName, scrollDirection));
        }

        // RESET FEED ENTRIES Y POSITION TO 0 
        feedEntriesTransform.localPosition = new Vector3(feedEntriesTransform.localPosition.x, 0, feedEntriesTransform.localPosition.z);

        // UNDO IGNORE LAYOUT ON FIRST ENTRY - HAPPENS WHEN SCROLLING UP
        var feedEntry = feedEntriesTransform.GetChild(0).gameObject;
        feedEntry.GetComponent<LayoutElement>().ignoreLayout = false;

        // IF SCROLLED TO NEW ENTRY, SWAP THE LAYER BEING VIEWED
        if (scrollDirection != 0)
            currentViewingLayer = (currentViewingLayer == "Dance1" ? "Dance2" : "Dance1");
    }

    private void DraggingFeed(int feedNum, float feedY)
    {
        // WHEN DRAGGING FEED, CREATE AND DESTROY ENTRIES TOP AND BOTTOM

        if (feedY == 0)
            return;

        var sceneName = presetFeedEntryNames[feedNum];
        var content = GetContentOfScene(sceneName);
        content.Stop();

        var previewingDirection = (int)Mathf.Sign(feedY);

        if (previewingDirection != lastPreviewingDirection)
        {
            lastPreviewingDirection = previewingDirection;

            // CLEAR ANY ENTRY THAT WE WERE PREVIEWING BEFORE
            if (previewingName != "")
            {
                StartCoroutine(UnloadSceneAsync(previewingName, lastPreviewingDirection));
                previewingName = "";
            }

            // DON'T PREVIEW THE TOP
            if (feedNum == 0 && previewingDirection == -1)
                return;

            var previewFeedTarget = (feedNum + previewingDirection);

            // LITTLE HACK TO MAKE THE FEED ENDLESS
            if (previewFeedTarget >= presetFeedEntryNames.Count)
            {
                presetFeedEntryNames.Add("ContentScene1");
                presetFeedEntryNames.Add("ContentScene2");
                presetFeedEntryNames.Add("ContentScene3");
            }

            // CREATE THE PREVIEW ENTRY
            previewingName = presetFeedEntryNames[previewFeedTarget];
            StartCoroutine(LoadNewScene(previewingName, previewingDirection, stop: true));
        }
    }

    private Content GetContentOfScene(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        GameObject[] gameObjects = scene.GetRootGameObjects();
        var content = gameObjects[0].GetComponent<Content>();
        
        return content;
    }

    private void DestroyEntryOnSide(int side)
    {
        var n = feedEntriesTransform.childCount - 1;
        GameObject firstChild = feedEntriesTransform.GetChild(side < 0 ? n : 0).gameObject;
        Destroy(firstChild);
    }

    private IEnumerator LoadNewScene(string sceneName, int scrollDirection = 0, bool stop = false)
    {
        // LOAD THE SCENE
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // PREPARE THE SCENE

        var content = GetContentOfScene(sceneName);

        if (stop)
        {
            content.Stop();
        }

        // DETERMINE LAYER TO USE
        string layerName = "";
        if (currentViewingLayer == "")
        {
            // SET TO DANCE1 FOR FIRST TIME
            layerName = "Dance1";
            currentViewingLayer = "Dance1";
        }
        else
        {
            layerName = (currentViewingLayer == "Dance1" ? "Dance2" : "Dance1");
        }

        var danceLayer = LayerMask.NameToLayer(layerName);

        // SET ALL OBJECTS OF AVATAR TO DANCE LAYER
        content.avatar.layer = danceLayer;
        foreach (Transform child in content.avatar.transform)
        {
            child.gameObject.layer = danceLayer;
        }

        // MAKE CAMERA ONLY SEE DANCE LAYER
        content.renderCamera.cullingMask = (1 << danceLayer);

        RenderTexture copiedTexture = new RenderTexture(renderTexture);
        content.renderCamera.targetTexture = copiedTexture;

        // CREATE THE FEED ENTRY
        var feedEntry = Instantiate(FeedEntryPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        feedEntry.transform.SetParent(feedEntriesTransform, false);

        if (scrollDirection == -1)
        {
            // SET ENTRY AT BEGINNING AND IGNORE IT, SO THAT WE KEEP VIEWING CURRENT ENTRY
            feedEntry.transform.SetAsFirstSibling();
            feedEntry.GetComponent<LayoutElement>().ignoreLayout = true;
            feedEntry.transform.localPosition = new Vector3(0, 1920 * .5f, 0);
        }

        // SET CAMERA'S RENDER TEXTURE TO THE FEED MATERIAL 
        var image = feedEntry.GetComponent<Image>();
        var originalMaterial = image.material;
        var newMaterial = new Material(originalMaterial);
        newMaterial.shader = originalMaterial.shader;
        newMaterial.SetTexture("_SubTex", copiedTexture);
        image.material = newMaterial;
    }

    private IEnumerator UnloadSceneAsync(string sceneName, int direction = 0)
    {
        DestroyEntryOnSide(direction);

        var scene = SceneManager.GetSceneByName(sceneName);
        GameObject[] gameObjects = scene.GetRootGameObjects();
        var content = gameObjects[0].transform;

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

        while (!asyncUnload.isDone)
        {
            yield return null;
        }
    }
}
