using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public class Tutorial
{
    // Rendering constants — same as Game
    private const int TileSize = 50;
    private const int OffsetX  = 25;
    private const int OffsetY  = 0;

    // Room pixel boundaries (inner walkable area)
    private float innerLeft   => OffsetX + TileSize;
    private float innerRight  => OffsetX + 14 * TileSize;
    private float innerTop    => OffsetY + TileSize;
    private float innerBottom => OffsetY + 8 * TileSize;

    // 5 rooms laid out horizontally: room 0 at grid (0,0), room 1 at (1,0) ... room 4 at (4,0)
    private int currentRoom = 0;
    private const int RoomCount = 5;

    // Player
    private Vector2 playerPos;
    private const float PlayerSize = 20;
    private float playerSpeed = 3.0f;
    private Vector2 lastMoveDir = new Vector2(0, 1);
    private float playerInvincTimer;
    private float playerFlashTimer;

    // Keys (set on Reset)
    private KeyboardKey upKey, downKey, leftKey, rightKey, actionKey;

    // Sword
    private Sword sword = new Sword(5, 1.5f, 2.0f);
    private float chargeHoldTimer;
    private Vector2 chargeHoldDir;

    // Room 5 enemy
    private Enemy? enemy;
    private List<Enemy> enemyList = new();
    private List<Bullet> bullets = new();

    // Room 5 gateway (back to menu)
    private bool showGateway;
    private Vector2 gatewayPos = new Vector2(400, 225);
    private const float GatewaySize = 20;

    // Room 4 items (5 types)
    private List<PickupItem> items = new();
    private bool[] itemCollected = new bool[5];

    // Language
    private int language;
    private Font font;

    // Knockback
    private Vector2 knockbackVel;

    // Empty room layout
    private static readonly int[,] EmptyLayout = new int[7, 13]
    {
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0,0,0,0}
    };

    public void SetFont(Font f) { font = f; }

    public void Reset(KeyboardKey up, KeyboardKey down, KeyboardKey left, KeyboardKey right, KeyboardKey action, int lang)
    {
        upKey = up; downKey = down; leftKey = left; rightKey = right; actionKey = action;
        language = lang;
        currentRoom = 0;
        playerPos = new Vector2(400, 225);
        lastMoveDir = new Vector2(0, 1);
        playerInvincTimer = 0; playerFlashTimer = 0;
        chargeHoldTimer = 0; chargeHoldDir = Vector2.Zero;
        knockbackVel = Vector2.Zero;
        sword = new Sword(5, 1.5f, 2.0f);
        bullets.Clear();
        enemyList.Clear();
        enemy = null;
        showGateway = false;
        SetupRoomItems();
    }

    private void SetupRoomItems()
    {
        items.Clear();
        itemCollected = new bool[5];
        // 5 items spread evenly across top of room 4
        ItemType[] types = { ItemType.MaxHealthUp, ItemType.SpeedUp, ItemType.DamageUp, ItemType.AttackSpeedUp, ItemType.RangeUp };
        float y = innerTop + TileSize * 1.5f;
        float totalW = innerRight - innerLeft;
        float step = totalW / 6f;
        for (int i = 0; i < 5; i++)
        {
            float x = innerLeft + step * (i + 1);
            items.Add(new PickupItem(new Vector2(x, y), types[i]));
        }
    }

    private void SpawnRoomEnemy()
    {
        enemyList.Clear();
        enemy = new Enemy(EnemyType.Walking, new Vector2(400, 150), playerSpeed);
        enemyList.Add(enemy);
    }

    // Returns true when player should go back to main menu
    public bool Update()
    {
        float dt = Raylib.GetFrameTime();

        // Tick iframes
        if (playerInvincTimer > 0) playerInvincTimer -= dt;
        if (playerFlashTimer  > 0) playerFlashTimer  -= dt;

        // Movement
        Vector2 moveDir = Vector2.Zero;
        if (Raylib.IsKeyDown(upKey))    moveDir.Y = -1;
        if (Raylib.IsKeyDown(downKey))  moveDir.Y =  1;
        if (Raylib.IsKeyDown(leftKey))  moveDir.X = -1;
        if (Raylib.IsKeyDown(rightKey)) moveDir.X =  1;
        if (moveDir.Length() > 0) lastMoveDir = Vector2.Normalize(moveDir);

        float spd = 3.0f * playerSpeed;
        Vector2 npX = new Vector2(playerPos.X + moveDir.X * spd, playerPos.Y);
        if (!WallCollision(npX)) playerPos = npX;
        Vector2 npY = new Vector2(playerPos.X, playerPos.Y + moveDir.Y * spd);
        if (!WallCollision(npY)) playerPos = npY;

        // Knockback
        if (knockbackVel.Length() > 0.5f)
        {
            Vector2 kx = new Vector2(knockbackVel.X * dt, 0);
            Vector2 ky = new Vector2(0, knockbackVel.Y * dt);
            if (!WallCollision(playerPos + kx)) playerPos += kx;
            if (!WallCollision(playerPos + ky)) playerPos += ky;
            knockbackVel *= 0.75f;
        }
        else knockbackVel = Vector2.Zero;

        // Sword
        sword.Update(dt, playerPos, Vector2.Zero, TileSize);

        Vector2 heldDir = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.Up))    heldDir = new Vector2(0, -1);
        else if (Raylib.IsKeyDown(KeyboardKey.Down))  heldDir = new Vector2(0,  1);
        else if (Raylib.IsKeyDown(KeyboardKey.Left))  heldDir = new Vector2(-1, 0);
        else if (Raylib.IsKeyDown(KeyboardKey.Right)) heldDir = new Vector2( 1, 0);

        if (heldDir.Length() > 0)
        {
            chargeHoldTimer += dt;
            chargeHoldDir = heldDir;
        }
        else
        {
            if (chargeHoldTimer > 0 && chargeHoldTimer < 0.4f)  sword.Attack(chargeHoldDir);
            else if (chargeHoldTimer >= 0.4f)                    sword.ChargeAttack(chargeHoldDir);
            chargeHoldTimer = 0;
            chargeHoldDir = Vector2.Zero;
        }

        // Room 5 (index 4): update enemy, bullets, gateway
        if (currentRoom == 4)
        {
            if (enemy == null) SpawnRoomEnemy();

            // Update enemies
            for (int i = 0; i < enemyList.Count; i++)
                enemyList[i].Update(dt, playerPos, EmptyLayout, TileSize, OffsetX, OffsetY, bullets, enemyList);
            enemyList.RemoveAll(e => !e.IsAlive);
            if (enemyList.Count == 0 && !showGateway) showGateway = true;

            // Bullets
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update(dt);
                if (Vector2.Distance(bullets[i].Position, playerPos) < PlayerSize / 2 + bullets[i].Size)
                {
                    TakeDamage();
                    bullets.RemoveAt(i);
                    continue;
                }
                if (bullets[i].Position.X < 0 || bullets[i].Position.X > 800 ||
                    bullets[i].Position.Y < 0 || bullets[i].Position.Y > 450)
                    bullets.RemoveAt(i);
            }

            // Enemy contact
            foreach (var e in enemyList)
            {
                if (!e.IsAlive) continue;
                if (Vector2.Distance(e.Position, playerPos) < e.Size + PlayerSize / 2 && e.CanContactDamage())
                {
                    TakeDamage();
                    knockbackVel = Vector2.Normalize(playerPos - e.Position) * 400f;
                }
            }

            // Sword hits enemy
            if (sword.IsAttacking())
            {
                var hb = sword.GetAttackHitbox();
                foreach (var e in enemyList)
                {
                    if (!e.IsAlive) continue;
                    if (PointToLineDistance(e.Position, hb.start, hb.end) < e.Size + hb.width / 2)
                    {
                        e.TakeDamage(5);
                        e.ApplyKnockback(Vector2.Normalize(e.Position - playerPos), 350f);
                    }
                }
            }
            if (sword.IsBoomerangActive())
            {
                Vector2 bp = sword.GetBoomerangPos();
                float br = sword.GetBoomerangHitRadius();
                foreach (var e in enemyList)
                {
                    if (!e.IsAlive) continue;
                    if (Vector2.Distance(bp, e.Position) < br + e.Size)
                    {
                        e.TakeDamage(5);
                        e.ApplyKnockback(Vector2.Normalize(e.Position - bp), 300f);
                    }
                }
            }

            // Gateway
            if (showGateway && Vector2.Distance(playerPos, gatewayPos) < PlayerSize / 2 + GatewaySize)
                return true; // back to main menu
        }

        // Room 4 (index 3): item pickups
        if (currentRoom == 3)
        {
            float dt2 = dt;
            foreach (var item in items) item.BobTimer += dt2;

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(playerPos, items[i].Position) < PlayerSize / 2 + 12f)
                {
                    items.RemoveAt(i);
                }
            }
        }

        // Room transitions — only right door exists between rooms
        // Right door: column 14, row 4 (center), pixel x = offsetX + 14*tileSize, y center
        float doorCenterY = (innerTop + innerBottom) / 2f;
        float doorHalfH   = TileSize / 2f;
        bool playerAtRightEdge  = playerPos.X > innerRight + 5  && playerPos.Y > doorCenterY - doorHalfH && playerPos.Y < doorCenterY + doorHalfH;
        bool playerAtLeftEdge   = playerPos.X < innerLeft  - 5  && playerPos.Y > doorCenterY - doorHalfH && playerPos.Y < doorCenterY + doorHalfH;

        if (playerAtRightEdge && currentRoom < RoomCount - 1)
        {
            currentRoom++;
            playerPos = new Vector2(innerLeft + PlayerSize + 5, 225);
            bullets.Clear();
            if (currentRoom == 4) { enemy = null; enemyList.Clear(); showGateway = false; }
        }
        else if (playerAtLeftEdge && currentRoom > 0)
        {
            currentRoom--;
            playerPos = new Vector2(innerRight - PlayerSize - 5, 225);
            bullets.Clear();
        }

        return false;
    }

    private void TakeDamage()
    {
        if (playerInvincTimer > 0) return;
        playerInvincTimer = 0.5f;
        playerFlashTimer  = 0.5f;
    }

    private bool WallCollision(Vector2 pos)
    {
        float left  = pos.X - PlayerSize / 2;
        float right = pos.X + PlayerSize / 2;
        float top   = pos.Y - PlayerSize / 2;
        float bot   = pos.Y + PlayerSize / 2;

        // Door gap is at vertical center of left/right walls
        float doorTop    = innerTop    + TileSize * 3;
        float doorBottom = innerBottom - TileSize * 3;

        bool hasLeftDoor  = currentRoom > 0;
        bool hasRightDoor = currentRoom < RoomCount - 1;

        if (top < innerTop)    return true;
        if (bot > innerBottom) return true;

        if (left < innerLeft)
        {
            bool gap = hasLeftDoor && pos.Y > doorTop && pos.Y < doorBottom;
            if (!gap) return true;
        }
        if (right > innerRight)
        {
            bool gap = hasRightDoor && pos.Y > doorTop && pos.Y < doorBottom;
            if (!gap) return true;
        }
        return false;
    }

    public void Draw()
    {
        Raylib.ClearBackground(new Color(40, 35, 30, 255));
        DrawRoom();
        DrawFloorText();
        if (currentRoom == 3) DrawItems();
        if (currentRoom == 4) DrawRoomFive();
        DrawPlayer();
        sword.Draw();
        Raylib.DrawText($"Room {currentRoom + 1}/5", 10, 10, 18, Color.White);
    }

    private void DrawRoom()
    {
        Color wall   = Color.DarkGray;
        Color floor  = new Color(80, 70, 60, 255);
        Color doorCol = Color.Beige;

        bool hasLeft  = currentRoom > 0;
        bool hasRight = currentRoom < RoomCount - 1;

        // Floor
        for (int x = 1; x < 14; x++)
            for (int y = 1; y < 8; y++)
                Raylib.DrawRectangle(OffsetX + x * TileSize, OffsetY + y * TileSize, TileSize - 1, TileSize - 1, floor);

        // Top wall
        for (int x = 0; x < 15; x++)
            Raylib.DrawRectangle(OffsetX + x * TileSize, OffsetY, TileSize - 1, TileSize - 1, wall);

        // Bottom wall
        for (int x = 0; x < 15; x++)
            Raylib.DrawRectangle(OffsetX + x * TileSize, OffsetY + 8 * TileSize, TileSize - 1, TileSize - 1, wall);

        // Left wall — with door gap if has left room
        for (int y = 0; y < 9; y++)
        {
            bool isDoor = hasLeft && y >= 4 && y <= 4; // door at row 4
            Raylib.DrawRectangle(OffsetX, OffsetY + y * TileSize, TileSize - 1, TileSize - 1,
                isDoor ? doorCol : wall);
        }

        // Right wall — with door gap if has right room
        for (int y = 0; y < 9; y++)
        {
            bool isDoor = hasRight && y >= 4 && y <= 4;
            Raylib.DrawRectangle(OffsetX + 14 * TileSize, OffsetY + y * TileSize, TileSize - 1, TileSize - 1,
                isDoor ? doorCol : wall);
        }
    }

    private string KeyName(KeyboardKey k) => k switch
    {
        KeyboardKey.W => "W", KeyboardKey.A => "A", KeyboardKey.S => "S", KeyboardKey.D => "D",
        KeyboardKey.Up => "[U]", KeyboardKey.Down => "[D]", KeyboardKey.Left => "[L]", KeyboardKey.Right => "[R]",
        KeyboardKey.Space => "Space", KeyboardKey.Enter => "Enter", KeyboardKey.Tab => "Tab",
        KeyboardKey.LeftShift => "Shift", KeyboardKey.RightShift => "Shift",
        KeyboardKey.LeftControl => "Ctrl", KeyboardKey.RightControl => "Ctrl",
        KeyboardKey.LeftAlt => "Alt", KeyboardKey.RightAlt => "Alt",
        _ => k.ToString().Replace("KEY_", "")
    };

    // Attack keys are always the 4 arrow keys
    private static readonly KeyboardKey[] AttackKeys =
    {
        KeyboardKey.Up, KeyboardKey.Down, KeyboardKey.Left, KeyboardKey.Right
    };

    private void DrawFloorText()
    {
        string line1, line2;
        Color textColor = new Color(220, 210, 180, 200);

        switch (currentRoom)
        {
            case 0:
                line1 = $"{KeyName(upKey)} {KeyName(downKey)} {KeyName(leftKey)} {KeyName(rightKey)}";
                line2 = Localization.Get("tutorial_move", language);
                break;
            case 1:
                line1 = $"{KeyName(AttackKeys[0])} {KeyName(AttackKeys[1])} {KeyName(AttackKeys[2])} {KeyName(AttackKeys[3])}";
                line2 = Localization.Get("tutorial_attack", language);
                break;
            case 2:
                line1 = Localization.Get("tutorial_ranged", language);
                line2 = "";
                break;
            case 3:
                line1 = Localization.Get("tutorial_items", language);
                line2 = "";
                break;
            case 4:
                line1 = Localization.Get("tutorial_enemy", language);
                line2 = "";
                break;
            default:
                return;
        }

        float y = innerBottom - 55;
        if (line1.Length > 0)
        {
            Vector2 sz = Raylib.MeasureTextEx(font, line1, 20, 1);
            Raylib.DrawTextEx(font, line1, new Vector2((800 - sz.X) / 2, y), 20, 1, textColor);
        }
        if (line2.Length > 0)
        {
            Vector2 sz2 = Raylib.MeasureTextEx(font, line2, 18, 1);
            Raylib.DrawTextEx(font, line2, new Vector2((800 - sz2.X) / 2, y + 24), 18, 1, textColor);
        }
    }

    private void DrawItems()
    {
        foreach (var item in items)
        {
            float bob = (float)Math.Sin(item.BobTimer * 3.0) * 3f;
            Vector2 drawPos = new Vector2(item.Position.X, item.Position.Y + bob);

            Color itemColor = item.Type switch
            {
                ItemType.MaxHealthUp   => Color.Red,
                ItemType.SpeedUp       => Color.Yellow,
                ItemType.DamageUp      => Color.Orange,
                ItemType.AttackSpeedUp => Color.SkyBlue,
                ItemType.RangeUp       => Color.Green,
                _                      => Color.White
            };

            // Pedestal
            Raylib.DrawRectangle((int)drawPos.X - 10, (int)drawPos.Y + 10, 20, 6, Color.DarkGray);
            Raylib.DrawCircleV(drawPos, 10f, itemColor);

            string label = item.Type switch
            {
                ItemType.MaxHealthUp   => "HP",
                ItemType.SpeedUp       => "SPD",
                ItemType.DamageUp      => "DMG",
                ItemType.AttackSpeedUp => "ATK",
                ItemType.RangeUp       => "RNG",
                _                      => "?"
            };
            Vector2 lsz = Raylib.MeasureTextEx(font, label, 11, 1);
            Raylib.DrawTextEx(font, label, new Vector2(drawPos.X - lsz.X / 2, drawPos.Y + 18), 11, 1, Color.White);
        }
    }

    private void DrawRoomFive()
    {
        foreach (var e in enemyList) e.Draw();
        foreach (var b in bullets)
            Raylib.DrawCircleV(b.Position, b.Size, Color.Yellow);

        if (showGateway)
        {
            float pulse = (float)Math.Sin(Raylib.GetTime() * 3.0) * 0.3f + 0.7f;
            Color gColor = new Color((byte)(100 * pulse), (byte)(200 * pulse), (byte)(255 * pulse), (byte)255);
            Raylib.DrawCircleV(gatewayPos, GatewaySize, gColor);
            Raylib.DrawCircleLines((int)gatewayPos.X, (int)gatewayPos.Y, (int)GatewaySize + 4, Color.White);
            string gText = Localization.Get("ui_gateway", language);
            Vector2 gsz = Raylib.MeasureTextEx(font, gText, 16, 1);
            Raylib.DrawTextEx(font, gText, new Vector2((800 - gsz.X) / 2, (int)gatewayPos.Y - 30), 16, 1, Color.White);
        }
    }

    private void DrawPlayer()
    {
        Color col = (playerFlashTimer > 0 && (int)(playerFlashTimer * 10) % 2 == 0) ? Color.Red : Color.Black;
        Raylib.DrawRectangle(
            (int)(playerPos.X - PlayerSize / 2),
            (int)(playerPos.Y - PlayerSize / 2),
            (int)PlayerSize, (int)PlayerSize, col);
    }

    private float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineDir = lineEnd - lineStart;
        float lineLen = lineDir.Length();
        if (lineLen < 0.001f) return Vector2.Distance(point, lineStart);
        float t = Math.Clamp(Vector2.Dot(point - lineStart, lineDir) / (lineLen * lineLen), 0f, 1f);
        Vector2 proj = lineStart + lineDir * t;
        return Vector2.Distance(point, proj);
    }
}