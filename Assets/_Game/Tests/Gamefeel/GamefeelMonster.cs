using System.Collections.Generic;
using UnityEngine;

namespace Jamie.Gamefeel
{
    public sealed class GamefeelMonster : MonoBehaviour
    {
        public GamefeelDirector director;
        public GamefeelNavigation navigation;
        public GameObject visual;
        public Vector2[] patrolPoints = new Vector2[0];
        public float patrolSpeed = 1.65f;
        public float roamingChaseSpeed = 3.8f;
        public float forcedChaseSeconds = 18f;
        public float damage = 20f;
        private enum Mode { Hidden, Reveal, ForcedChase, Roaming }
        private Mode mode;
        private List<Vector2> path = new List<Vector2>();
        private int waypoint;
        private int patrolIndex;
        private float nextPath;
        private float chaseStarted;
        private float nextHit;
        private float idleUntil;
        private Vector2 lastGoal;
        public bool IsRoaming => mode == Mode.Roaming;
        public bool IsForcedChase => mode == Mode.ForcedChase;

        private void LateUpdate()
        {
            if (visual == null || director == null) return;
            bool visible = mode != Mode.Hidden && (mode == Mode.Reveal ||
                director.HasLineOfSight(director.Player.transform.position, transform));
            if (visual.activeSelf != visible) visual.SetActive(visible);
        }

        public void Hide()
        {
            mode = Mode.Hidden; path.Clear();
            if (visual != null) visual.SetActive(false);
        }

        public void Reveal(Vector2 position)
        {
            mode = Mode.Reveal; transform.position = position;
            if (visual != null) visual.SetActive(true);
        }

        public void StartForcedChase()
        {
            mode = Mode.ForcedChase; chaseStarted = Time.time;
            nextPath = 0; nextHit = Time.time + 2.4f; path.Clear();
            if (visual != null) visual.SetActive(true);
        }

        public void StartRoaming(Vector2 position)
        {
            transform.position = position;
            mode = Mode.Roaming; patrolIndex = 0; nextPath = 0;
            nextHit = Time.time + 2f; idleUntil = 0; path.Clear();
            if (visual != null) visual.SetActive(true);
        }

        private void Update()
        {
            if (director == null || navigation == null || director.IsBusy || director.TestComplete ||
                Time.timeScale <= 0 || mode == Mode.Hidden || mode == Mode.Reveal) return;
            if (Time.time < idleUntil) return;
            Vector2 player = director.Player.transform.position;
            Vector2 current = transform.position;
            bool forced = mode == Mode.ForcedChase;
            if (forced && Time.time - chaseStarted >= forcedChaseSeconds)
            { director.ReportStoryCatch(); return; }
            bool seesPlayer = forced || navigation.IsInHallway(player) &&
                Vector2.Distance(current, player) < (director.FlashlightOn ? 10f : 5f) &&
                director.HasLineOfSight(current, director.Player.transform);
            Vector2 goal = seesPlayer ? player : patrolPoints.Length > 0 ? patrolPoints[patrolIndex] : current;
            if (Time.time >= nextPath && (path.Count == 0 || Vector2.Distance(goal, lastGoal) > 0.6f))
            {
                path = navigation.FindPath(current, goal, !forced); waypoint = 0;
                lastGoal = goal; nextPath = Time.time + 0.45f;
            }
            float speed = forced ? Mathf.Lerp(3.2f, 6.1f, (Time.time - chaseStarted) / forcedChaseSeconds)
                : seesPlayer ? roamingChaseSpeed : patrolSpeed;
            if (waypoint < path.Count)
            {
                Vector2 next = Vector2.MoveTowards(current, path[waypoint], speed * Time.deltaTime);
                if (!forced && !navigation.IsInHallway(next)) { path.Clear(); return; }
                Vector2 direction = next - current;
                transform.position = next;
                if (visual != null && direction.sqrMagnitude > 0.0001f)
                    visual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
                if (Vector2.Distance(next, path[waypoint]) < 0.07f) waypoint++;
            }
            else
            {
                path.Clear();
                if (!seesPlayer && patrolPoints.Length > 0) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
            if (Vector2.Distance(transform.position, player) < 1.05f && Time.time >= nextHit &&
                (forced || navigation.IsInHallway(player)))
            {
                if (forced) director.ReportStoryCatch();
                else
                {
                    director.ReportMonsterHit(damage); nextHit = Time.time + 2f;
                    idleUntil = Time.time + 0.8f; path.Clear();
                    if (patrolPoints.Length > 0) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                }
            }
        }
    }
}
