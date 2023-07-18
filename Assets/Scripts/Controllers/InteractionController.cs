using UnityEngine;
using DG.Tweening;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Transform feedEntriesTransform;
    [SerializeField] private float minPixelsYForScroll = 5f;
    [SerializeField] private float scrollDuration = .35f;

    private bool pointerIsDown = false;
    private float pointerDownY = 0;
    private float feedStartY = 0;
    private float pointerLastY = 0;
    private float pointerSpeedY = 0;
    private float pointerSpeedYLerpTo0 = 0;
    private int currentFeedNum = 0;

    // HOOKED UP AT CANVAS->FEED->ON POINTER DOWN
    private void onPointerDownFeed()
    {
        // PREVENTING SOME BUGS BY NOT ALLOWING SCROLLING WHILE TWEENING
        if (DOTween.IsTweening(feedEntriesTransform))
        {
            //DOTween.Kill(feedEntriesTransform); // <- SHOULD BE
            return;
        }

        pointerIsDown = true;
        feedStartY = feedEntriesTransform.position.y;
        pointerDownY = Input.mousePosition.y;
        pointerLastY = pointerDownY;
    }

    // HOOKED UP AT CANVAS->FEED->ON POINTER UP
    private void onPointerUpFeed()
    {
        if (!pointerIsDown)
        {
            return;
        }

        var direction = 0;

        // IF MOVING/SWIPING FAST ENOUGH, SET CORESPONDING SWIPE DIRECTION
        if (Mathf.Abs(pointerSpeedYLerpTo0) > minPixelsYForScroll)
        {
            direction = (int)Mathf.Sign(pointerSpeedYLerpTo0);
        }

        // CHECK IF SWIPING INTO CURRENT FEED (WHEN DRAGGING OUT AND SWIPING BACK IN) - THEN SET DIRECTION TO 0, SO IT WILL SCROLL BACK TO CENTER
        var dragDirection = (int)Mathf.Sign(feedEntriesTransform.localPosition.y);
        if (direction != dragDirection)
            direction = 0;

        // IF DRAGGED FURTHER THAN HALFWAY THE SCREEN, SET CORESPONDING DIRECTION
        if (Mathf.Abs(feedEntriesTransform.localPosition.y) > 1920 * .5)
        {
            direction = dragDirection;
        }

        ScrollFeed(direction);

        pointerIsDown = false;
        pointerSpeedY = 0;
        pointerSpeedYLerpTo0 = 0;
    }

    private void ScrollFeed(int scrollDirection = 1)
    {
        // SCROLL THE FEED TO THE GIVEN DIRECTION - 0 BEING BACK TO CENTER

        var feedNumTarget = (currentFeedNum + scrollDirection);
        feedNumTarget = Mathf.Max(0, feedNumTarget);

        var prevFeedNum = currentFeedNum;
        currentFeedNum = feedNumTarget;
        var changedFeedNum = (prevFeedNum != currentFeedNum);

        var feedTargetY = 1920 * scrollDirection;

        if (prevFeedNum == 0 && scrollDirection == -1)
            feedTargetY = 0;

        // IF NOT CHANGING FEED, SET DIRECTION TO 0, SO EVENTS CAN TREAT IT AS A 'NO-SCROLL'
        if (!changedFeedNum)
            scrollDirection = 0;

        feedEntriesTransform.DOLocalMoveY(feedTargetY, scrollDuration)
            .SetAutoKill(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                Events.InvokeScrollFeedEnd(currentFeedNum, scrollDirection);
            });

        Events.InvokeScrollFeedStart(currentFeedNum, scrollDirection);
    }

    void Update()
    {
        // HANDLE FEED ENTRIES DRAG ON POINER DOWN

        if (pointerIsDown)
        {
            // MOVE IT
            var deltaY = (Input.mousePosition.y - pointerDownY);
            feedEntriesTransform.position = new Vector3(feedEntriesTransform.position.x, feedStartY + deltaY, feedEntriesTransform.position.z);

            // HANDLE POINTER SPEEDS
            pointerSpeedY = (Input.mousePosition.y - pointerLastY);
            pointerSpeedYLerpTo0 *= .8f;

            // SET pointerSpeedYLerpTo0 EQUAL TO pointerSpeedY IF pointerSpeedY IS 'BIGGER'
            if (pointerSpeedY < 0)
                pointerSpeedYLerpTo0 = Mathf.Min(pointerSpeedYLerpTo0, pointerSpeedY);
            if (pointerSpeedY > 0)
                pointerSpeedYLerpTo0 = Mathf.Max(pointerSpeedYLerpTo0, pointerSpeedY);

            pointerLastY = Input.mousePosition.y;

            Events.InvokeDraggingFeed(currentFeedNum, feedEntriesTransform.localPosition.y);
        }
    }
}
