using System.Collections.Generic;
using UnityEngine;

namespace Jamie.Gamefeel
{
    public sealed class GamefeelNavigation : MonoBehaviour
    {
        public Rect bounds = new Rect(-54, -32, 92, 60);
        public float cellSize = 0.65f;
        public Rect[] floorAreas = new Rect[0];
        public Rect[] hallwayAreas = new Rect[0];
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);
        private int Width => Mathf.CeilToInt(bounds.width / cellSize);
        private int Height => Mathf.CeilToInt(bounds.height / cellSize);
        private int Cell(Vector2 p) => Mathf.Clamp(Mathf.FloorToInt((p.y - bounds.yMin) / cellSize), 0, Height - 1) * Width +
            Mathf.Clamp(Mathf.FloorToInt((p.x - bounds.xMin) / cellSize), 0, Width - 1);
        private Vector2 Point(int index) => new Vector2(bounds.xMin + (index % Width + 0.5f) * cellSize,
            bounds.yMin + (index / Width + 0.5f) * cellSize);

        public bool IsInHallway(Vector2 p)
        {
            foreach (Rect rect in hallwayAreas) if (rect.Contains(p)) return true;
            return false;
        }

        private bool Walkable(int index, bool hallOnly, Dictionary<int, bool> cache)
        {
            if (cache.TryGetValue(index, out bool known)) return known;
            Vector2 p = Point(index);
            bool onFloor = false;
            foreach (Rect rect in hallOnly ? hallwayAreas : floorAreas)
                if (rect.Contains(p)) { onFloor = true; break; }
            if (!onFloor) { cache[index] = false; return false; }
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask((1 << 0) | (1 << 6));
            filter.useTriggers = false;
            overlaps.Clear();
            Physics2D.OverlapCircle(p, 0.29f, filter, overlaps);
            foreach (Collider2D collider in overlaps)
                if (collider.GetComponentInParent<PlayerController>() == null)
                { cache[index] = false; return false; }
            cache[index] = true;
            return true;
        }

        private int Nearest(int cell, bool hallOnly, Dictionary<int, bool> cache)
        {
            if (Walkable(cell, hallOnly, cache)) return cell;
            int cx = cell % Width, cy = cell / Width;
            for (int radius = 1; radius <= 4; radius++)
                for (int y = -radius; y <= radius; y++)
                    for (int x = -radius; x <= radius; x++)
                    {
                        int nx = cx + x, ny = cy + y;
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        int candidate = ny * Width + nx;
                        if (Walkable(candidate, hallOnly, cache)) return candidate;
                    }
            return -1;
        }

        private struct Entry { public int cell; public float score; }
        private sealed class Heap
        {
            private readonly List<Entry> items = new List<Entry>();
            public int Count => items.Count;
            public void Push(int cell, float score)
            {
                Entry value = new Entry { cell = cell, score = score };
                items.Add(value); int index = items.Count - 1;
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (items[parent].score <= value.score) break;
                    items[index] = items[parent]; index = parent;
                }
                items[index] = value;
            }
            public int Pop()
            {
                int result = items[0].cell;
                Entry tail = items[items.Count - 1]; items.RemoveAt(items.Count - 1);
                if (items.Count == 0) return result;
                int index = 0;
                while (index * 2 + 1 < items.Count)
                {
                    int child = index * 2 + 1;
                    if (child + 1 < items.Count && items[child + 1].score < items[child].score) child++;
                    if (tail.score <= items[child].score) break;
                    items[index] = items[child]; index = child;
                }
                items[index] = tail; return result;
            }
        }

        public List<Vector2> FindPath(Vector2 start, Vector2 destination, bool hallOnly = false)
        {
            Physics2D.SyncTransforms();
            var cache = new Dictionary<int, bool>();
            int source = Nearest(Cell(start), hallOnly, cache), goal = Nearest(Cell(destination), hallOnly, cache);
            var path = new List<Vector2>();
            if (source < 0 || goal < 0) return path;
            var open = new Heap();
            var costs = new Dictionary<int, float> { [source] = 0 };
            var parents = new Dictionary<int, int>();
            var closed = new HashSet<int>();
            open.Push(source, Vector2.Distance(Point(source), Point(goal)));
            int examined = 0;
            while (open.Count > 0 && examined++ < 12000)
            {
                int current = open.Pop();
                if (!closed.Add(current)) continue;
                if (current == goal)
                {
                    for (int cell = goal; cell != source; cell = parents[cell]) path.Add(Point(cell));
                    path.Reverse();
                    if (path.Count == 0) path.Add(Point(goal));
                    return path;
                }
                int cx = current % Width, cy = current / Width;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    int nx = cx + x, ny = cy + y;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    int next = ny * Width + nx;
                    if (closed.Contains(next) || !Walkable(next, hallOnly, cache)) continue;
                    if (x != 0 && y != 0 && (!Walkable(cy * Width + nx, hallOnly, cache) ||
                        !Walkable(ny * Width + cx, hallOnly, cache))) continue;
                    float cost = costs[current] + (x != 0 && y != 0 ? 1.414214f : 1f) * cellSize;
                    if (costs.TryGetValue(next, out float previous) && previous <= cost) continue;
                    costs[next] = cost; parents[next] = current;
                    open.Push(next, cost + Vector2.Distance(Point(next), Point(goal)));
                }
            }
            return path;
        }
    }
}
