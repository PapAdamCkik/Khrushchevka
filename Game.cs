using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Khrushchevka_RPG;

class Game
{
    private FloorGenerator floorGenerator;
    private Dictionary<GridPosition, FloorNode> currentFloor;
    private GridPosition currentRoomPos;
    private FloorNode? currentRoomNode;
    
    // Floor tracking
    private int currentFloorNumber;
    private int maxUnlockedFloor;
    
    // Player variables
    private Vector2 playerPos;
    private float playerSize;
    private float playerSpeed;
    
    // Room rendering variables
    private int tileSize;
    private int offsetX;
    private int offsetY;
    
    // Gateway variables
    private Vector2 gatewayPos;
    private float gatewaySize;
    private bool showGateway;
    
    // Pause menu variables
    private bool isPaused;
    private int pauseSelectedOption;
    private string[] pauseOptions;
    private Font pauseFont;
    
    public Game()
    {
        // Initialize room blueprints
        RoomBlueprints.Initialize();
        
        currentFloorNumber = 1;
        maxUnlockedFloor = 7;  // Floors 1-7 unlocked by default
        
        // Generate first floor (PREGENERATED)
        floorGenerator = new FloorGenerator();
        currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
        currentRoomPos = new GridPosition(0, 0);  // Start at spawn
        currentRoomNode = currentFloor[currentRoomPos];
        
        // Calculate room layout
        CalculateRoomLayout();
        
        // Initialize player in center of room
        playerPos = new Vector2(400, 225);  // Center of screen
        playerSize = 20;  // Smaller than a tile
        playerSpeed = 3.0f;
        
        // Initialize gateway
        gatewaySize = 40;
        showGateway = false;
        
        // Initialize pause menu
        isPaused = false;
        pauseSelectedOption = 0;
        pauseOptions = new string[] { "Return to Game", "Settings", "Return to Main Menu" };
    }
    
    public void SetFont(Font font)
    {
        pauseFont = font;
    }
    
    public void LoadProgress(Dictionary<string, bool> unlocks)
    {
        // Check if floor 7 boss has been defeated
        if (unlocks.ContainsKey("floor_7_boss") && unlocks["floor_7_boss"])
        {
            maxUnlockedFloor = 9;  // Unlock floors 8 and 9
        }
    }
    
    private void CalculateRoomLayout()
    {
        // Calculate tile size to fit full screen
        int tileSizeX = 800 / 15;
        int tileSizeY = 450 / 9;
        tileSize = Math.Min(tileSizeX, tileSizeY);
        
        // Center the room
        int totalWidth = tileSize * 15;
        int totalHeight = tileSize * 9;
        offsetX = (800 - totalWidth) / 2;
        offsetY = (450 - totalHeight) / 2;
    }
    
    private void UpdateGateway(Dictionary<string, bool> unlocks)
    {
        // Check if we're in boss room
        if (currentRoomNode?.Type == RoomType.Boss)
        {
            // For now, always show gateway (until bosses are implemented)
            // Later: check if boss is defeated
            // bool bossDefeated = unlocks.ContainsKey($"floor_{currentFloorNumber}_boss") && unlocks[$"floor_{currentFloorNumber}_boss"];
            bool bossDefeated = true;  // Always show for now
            
            if (bossDefeated && currentFloorNumber < maxUnlockedFloor)
            {
                showGateway = true;
                // Place gateway in center of room
                gatewayPos = new Vector2(400, 225);
            }
            else
            {
                showGateway = false;
            }
        }
        else
        {
            showGateway = false;
        }
    }
    
    private void CheckGatewayInteraction()
    {
        if (!showGateway)
            return;
        
        // Check if player is touching gateway
        float distance = Vector2.Distance(playerPos, gatewayPos);
        if (distance < (playerSize + gatewaySize) / 2)
        {
            // Player can advance to next floor
            if (currentFloorNumber < maxUnlockedFloor)
            {
                GoToNextFloor();
            }
        }
    }
    
    public (Program.GameState newState, bool openSettings) Update(KeyboardKey upKey, KeyboardKey downKey, KeyboardKey leftKey, KeyboardKey rightKey, KeyboardKey actionKey, int difficulty, int language, Dictionary<string, bool> unlocks)
    {
        // Toggle pause with Escape
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            isPaused = !isPaused;
            pauseSelectedOption = 0;  // Reset selection
        }
        
        if (isPaused)
        {
            // Pause menu controls
            if (Raylib.IsKeyPressed(downKey))
                pauseSelectedOption = (pauseSelectedOption + 1) % pauseOptions.Length;
            
            if (Raylib.IsKeyPressed(upKey))
                pauseSelectedOption = (pauseSelectedOption - 1 + pauseOptions.Length) % pauseOptions.Length;
            
            if (Raylib.IsKeyPressed(actionKey))
            {
                switch (pauseSelectedOption)
                {
                    case 0: // Return to Game
                        isPaused = false;
                        break;
                    case 1: // Settings
                        return (Program.GameState.Settings, true);
                    case 2: // Return to Main Menu
                        return (Program.GameState.MainMenu, false);
                }
            }
        }
        else
        {
            // Game is not paused - normal gameplay
            
            // Press R to regenerate current floor (for testing)
            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
                currentRoomPos = new GridPosition(0, 0);
                currentRoomNode = currentFloor[currentRoomPos];
                playerPos = new Vector2(400, 225);
            }
            
            // Press P to go to previous floor (for testing)
            if (Raylib.IsKeyPressed(KeyboardKey.P) && currentFloorNumber > 1)
            {
                GoToPreviousFloor();
            }
            
            // Player movement
            Vector2 newPos = playerPos;
            
            if (Raylib.IsKeyDown(upKey))
                newPos.Y -= playerSpeed;
            if (Raylib.IsKeyDown(downKey))
                newPos.Y += playerSpeed;
            if (Raylib.IsKeyDown(leftKey))
                newPos.X -= playerSpeed;
            if (Raylib.IsKeyDown(rightKey))
                newPos.X += playerSpeed;
            
            // Check collision before moving
            if (!CheckCollision(newPos))
            {
                playerPos = newPos;
            }
            
            // Check for room transitions through doors
            CheckRoomTransition();
            
            // Update gateway visibility
            UpdateGateway(unlocks);
            
            // Check if player touched gateway
            CheckGatewayInteraction();
            
            // Check if player defeated boss in boss room
            CheckBossDefeat(unlocks);
        }
        
        return (Program.GameState.Playing, false);
    }
    
    private void GoToNextFloor()
    {
        if (currentFloorNumber < maxUnlockedFloor)
        {
            currentFloorNumber++;
            currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
            currentRoomPos = new GridPosition(0, 0);
            currentRoomNode = currentFloor[currentRoomPos];
            playerPos = new Vector2(400, 225);
            showGateway = false;  // Reset gateway
        }
    }
    
    private void GoToPreviousFloor()
    {
        if (currentFloorNumber > 1)
        {
            currentFloorNumber--;
            currentFloor = floorGenerator.GenerateFloor(currentFloorNumber);
            currentRoomPos = new GridPosition(0, 0);
            currentRoomNode = currentFloor[currentRoomPos];
            playerPos = new Vector2(400, 225);
            showGateway = false;  // Reset gateway
        }
    }
    
    private void CheckBossDefeat(Dictionary<string, bool> unlocks)
    {
        // If in boss room and boss is defeated (placeholder - you'll add real boss fight later)
        if (currentRoomNode?.Type == RoomType.Boss)
        {
            // Example: Press B to "defeat" boss (for testing)
            if (Raylib.IsKeyPressed(KeyboardKey.B))
            {
                // Mark floor boss as defeated
                string bossKey = $"floor_{currentFloorNumber}_boss";
                if (unlocks.ContainsKey(bossKey))
                    unlocks[bossKey] = true;
                else
                    unlocks.Add(bossKey, true);
                
                // If floor 7 boss defeated, unlock floors 8 and 9
                if (currentFloorNumber == 7)
                {
                    maxUnlockedFloor = 9;
                }
                
                // Save progress
                SettingsManager.SaveProgress(unlocks);
                
                // Update gateway visibility
                UpdateGateway(unlocks);
            }
        }
    }
    
    private void CheckRoomTransition()
    {
        if (currentRoomNode == null)
            return;
        
        int roomLeft = offsetX + tileSize;
        int roomRight = offsetX + 14 * tileSize;
        int roomTop = offsetY + tileSize;
        int roomBottom = offsetY + 8 * tileSize;
        
        // Door positions
        int topDoorLeft = offsetX + 7 * tileSize;
        int topDoorRight = offsetX + 8 * tileSize;
        int topDoorY = offsetY;
        
        int bottomDoorLeft = offsetX + 7 * tileSize;
        int bottomDoorRight = offsetX + 8 * tileSize;
        int bottomDoorY = offsetY + 8 * tileSize;
        
        int leftDoorX = offsetX;
        int leftDoorTop = offsetY + 4 * tileSize;
        int leftDoorBottom = offsetY + 5 * tileSize;
        
        int rightDoorX = offsetX + 14 * tileSize;
        int rightDoorTop = offsetY + 4 * tileSize;
        int rightDoorBottom = offsetY + 5 * tileSize;
        
        // Check if player is going through top door
        if (currentRoomNode.HasTopDoor && playerPos.Y < topDoorY + tileSize / 2 &&
            playerPos.X > topDoorLeft && playerPos.X < topDoorRight)
        {
            TransitionToRoom(0, -1, "bottom");
        }
        
        // Check if player is going through bottom door
        if (currentRoomNode.HasBottomDoor && playerPos.Y > bottomDoorY + tileSize / 2 &&
            playerPos.X > bottomDoorLeft && playerPos.X < bottomDoorRight)
        {
            TransitionToRoom(0, 1, "top");
        }
        
        // Check if player is going through left door
        if (currentRoomNode.HasLeftDoor && playerPos.X < leftDoorX + tileSize / 2 &&
            playerPos.Y > leftDoorTop && playerPos.Y < leftDoorBottom)
        {
            TransitionToRoom(-1, 0, "right");
        }
        
        // Check if player is going through right door
        if (currentRoomNode.HasRightDoor && playerPos.X > rightDoorX + tileSize / 2 &&
            playerPos.Y > rightDoorTop && playerPos.Y < rightDoorBottom)
        {
            TransitionToRoom(1, 0, "left");
        }
    }
    
    private void TransitionToRoom(int deltaX, int deltaY, string entryDirection)
    {
        // Calculate new room position
        GridPosition newRoomPos = new GridPosition(currentRoomPos.X + deltaX, currentRoomPos.Y + deltaY);
        
        // Check if room exists
        if (currentFloor.ContainsKey(newRoomPos))
        {
            currentRoomPos = newRoomPos;
            currentRoomNode = currentFloor[currentRoomPos];
            
            // Position player at opposite door
            switch (entryDirection)
            {
                case "top":
                    playerPos = new Vector2(400, offsetY + tileSize + 30);
                    break;
                case "bottom":
                    playerPos = new Vector2(400, offsetY + 8 * tileSize - 30);
                    break;
                case "left":
                    playerPos = new Vector2(offsetX + tileSize + 30, 225);
                    break;
                case "right":
                    playerPos = new Vector2(offsetX + 14 * tileSize - 30, 225);
                    break;
            }
        }
    }
    
    private bool CheckCollision(Vector2 newPos)
    {
        if (currentRoomNode?.RoomData == null)
            return true;
        
        // Player bounds
        float playerLeft = newPos.X - playerSize / 2;
        float playerRight = newPos.X + playerSize / 2;
        float playerTop = newPos.Y - playerSize / 2;
        float playerBottom = newPos.Y + playerSize / 2;
        
        // Check collision with walls
        int roomLeft = offsetX + tileSize;
        int roomRight = offsetX + 14 * tileSize;
        int roomTop = offsetY + tileSize;
        int roomBottom = offsetY + 8 * tileSize;
        
        // Door positions and sizes
        int topDoorLeft = offsetX + 7 * tileSize;
        int topDoorRight = offsetX + 8 * tileSize;
        int topDoorY = offsetY;
        
        int bottomDoorLeft = offsetX + 7 * tileSize;
        int bottomDoorRight = offsetX + 8 * tileSize;
        int bottomDoorY = offsetY + 8 * tileSize;
        
        int leftDoorX = offsetX;
        int leftDoorTop = offsetY + 4 * tileSize;
        int leftDoorBottom = offsetY + 5 * tileSize;
        
        int rightDoorX = offsetX + 14 * tileSize;
        int rightDoorTop = offsetY + 4 * tileSize;
        int rightDoorBottom = offsetY + 5 * tileSize;
        
        // Wall collision with door exceptions
        // Top wall
        if (playerTop < roomTop)
        {
            if (currentRoomNode.HasTopDoor && playerLeft < topDoorRight && playerRight > topDoorLeft)
            {
                // In door area, allow passage
            }
            else
            {
                return true;
            }
        }
        
        // Bottom wall
        if (playerBottom > roomBottom)
        {
            if (currentRoomNode.HasBottomDoor && playerLeft < bottomDoorRight && playerRight > bottomDoorLeft)
            {
                // In door area, allow passage
            }
            else
            {
                return true;
            }
        }
        
        // Left wall
        if (playerLeft < roomLeft)
        {
            if (currentRoomNode.HasLeftDoor && playerTop < leftDoorBottom && playerBottom > leftDoorTop)
            {
                // In door area, allow passage
            }
            else
            {
                return true;
            }
        }
        
        // Right wall
        if (playerRight > roomRight)
        {
            if (currentRoomNode.HasRightDoor && playerTop < rightDoorBottom && playerBottom > rightDoorTop)
            {
                // In door area, allow passage
            }
            else
            {
                return true;
            }
        }
        
        // Check collision with obstacles in room layout
        int[,] layout = currentRoomNode.RoomData.Layout;
        
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 13; x++)
            {
                int tile = layout[y, x];
                
                if (tile == 2)
                {
                    int tileLeft = offsetX + (x + 1) * tileSize;
                    int tileRight = offsetX + (x + 2) * tileSize;
                    int tileTop = offsetY + (y + 1) * tileSize;
                    int tileBottom = offsetY + (y + 2) * tileSize;
                    
                    if (playerRight > tileLeft && playerLeft < tileRight &&
                        playerBottom > tileTop && playerTop < tileBottom)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    public void Draw()
    {
        if (currentRoomNode != null && currentRoomNode.RoomData != null)
        {
            DrawRoom(currentRoomNode);
            
            // Draw gateway if visible
            if (showGateway)
            {
                DrawGateway();
            }
            
            DrawPlayer();
            DrawMinimap();
        }
        
        if (isPaused)
        {
            DrawPauseMenu();
        }
        else
        {
            // Draw UI
            Raylib.DrawText($"Floor {currentFloorNumber}", 10, 10, 20, Color.White);
            Raylib.DrawText("Press P for prev floor", 10, 35, 14, Color.White);
            Raylib.DrawText("Press B in boss room to defeat", 10, 55, 14, Color.White);
            Raylib.DrawText("Press ESC to pause", 10, 75, 14, Color.White);
            
            // Show unlock status
            if (maxUnlockedFloor >= 9)
            {
                Raylib.DrawText("Floors 8-9 UNLOCKED!", 10, 95, 14, Color.Gold);
            }
            
            // Show gateway hint
            if (showGateway)
            {
                Raylib.DrawText("Gateway to next floor!", 250, 400, 20, Color.Gold);
            }
        }
    }
    
    private void DrawGateway()
    {
        // Draw brown square as gateway
        Raylib.DrawRectangle(
            (int)(gatewayPos.X - gatewaySize / 2),
            (int)(gatewayPos.Y - gatewaySize / 2),
            (int)gatewaySize,
            (int)gatewaySize,
            new Color(139, 69, 19, 255)  // Brown color
        );
        
        // Draw golden border to make it stand out
        Raylib.DrawRectangleLines(
            (int)(gatewayPos.X - gatewaySize / 2),
            (int)(gatewayPos.Y - gatewaySize / 2),
            (int)gatewaySize,
            (int)gatewaySize,
            Color.Gold
        );
    }
    
    private void DrawMinimap()
    {
        int minimapTileSize = 15;
        int minimapX = 800 - 20;
        int minimapY = 20;
        
        int minX = currentFloor.Keys.Min(p => p.X);
        int maxX = currentFloor.Keys.Max(p => p.X);
        int minY = currentFloor.Keys.Min(p => p.Y);
        int maxY = currentFloor.Keys.Max(p => p.Y);
        
        int mapWidth = (maxX - minX + 1) * minimapTileSize + 10;
        int mapHeight = (maxY - minY + 1) * minimapTileSize + 10;
        Raylib.DrawRectangle(minimapX - mapWidth, minimapY, mapWidth, mapHeight, new Color(0, 0, 0, 150));
        
        foreach (var room in currentFloor.Values)
        {
            int roomX = minimapX - mapWidth + 5 + (room.Position.X - minX) * minimapTileSize;
            int roomY = minimapY + 5 + (room.Position.Y - minY) * minimapTileSize;
            
            Color roomColor = new Color(128, 128, 128, 200);
            
            if (room.Type == RoomType.Start)
                roomColor = new Color(0, 255, 0, 200);
            else if (room.Type == RoomType.Item)
                roomColor = new Color(255, 215, 0, 200);
            else if (room.Type == RoomType.Shop)
                roomColor = new Color(0, 191, 255, 200);
            else if (room.Type == RoomType.Boss)
                roomColor = new Color(255, 0, 0, 200);
            
            if (room.Position.Equals(currentRoomPos))
                roomColor = new Color(255, 255, 255, 255);
            
            Raylib.DrawRectangle(roomX, roomY, minimapTileSize - 2, minimapTileSize - 2, roomColor);
        }
    }
    
    private void DrawPauseMenu()
    {
        Raylib.DrawRectangle(0, 0, 800, 450, new Color(0, 0, 0, 180));
        Raylib.DrawTextEx(pauseFont, "PAUSED", new Vector2(330, 80), 40, 2, Color.White);
        
        for (int i = 0; i < pauseOptions.Length; i++)
        {
            Rectangle buttonRect = new Rectangle(250, 180 + i * 60, 300, 50);
            Color bgColor = i == pauseSelectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == pauseSelectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            
            Vector2 textSize = Raylib.MeasureTextEx(pauseFont, pauseOptions[i], 24, 2);
            float textX = buttonRect.X + (buttonRect.Width - textSize.X) / 2;
            float textY = buttonRect.Y + (buttonRect.Height - textSize.Y) / 2;
            
            Raylib.DrawTextEx(pauseFont, pauseOptions[i], new Vector2(textX, textY), 24, 2, textColor);
        }
    }
    
    private void DrawPlayer()
    {
        Raylib.DrawRectangle(
            (int)(playerPos.X - playerSize / 2),
            (int)(playerPos.Y - playerSize / 2),
            (int)playerSize,
            (int)playerSize,
            Color.Black
        );
    }
    
    private void DrawRoom(FloorNode node)
    {
        // Draw walls
        for (int x = 0; x < 15; x++)
        {
            if (x == 7 && node.HasTopDoor)
                Raylib.DrawRectangle(offsetX + x * tileSize, offsetY, tileSize - 1, tileSize - 1, Color.Beige);
            else
                Raylib.DrawRectangle(offsetX + x * tileSize, offsetY, tileSize - 1, tileSize - 1, Color.DarkGray);
        }
        
        for (int x = 0; x < 15; x++)
        {
            if (x == 7 && node.HasBottomDoor)
                Raylib.DrawRectangle(offsetX + x * tileSize, offsetY + 8 * tileSize, tileSize - 1, tileSize - 1, Color.Beige);
            else
                Raylib.DrawRectangle(offsetX + x * tileSize, offsetY + 8 * tileSize, tileSize - 1, tileSize - 1, Color.DarkGray);
        }
        
        for (int y = 0; y < 9; y++)
        {
            if (y == 4 && node.HasLeftDoor)
                Raylib.DrawRectangle(offsetX, offsetY + y * tileSize, tileSize - 1, tileSize - 1, Color.Beige);
            else
                Raylib.DrawRectangle(offsetX, offsetY + y * tileSize, tileSize - 1, tileSize - 1, Color.DarkGray);
        }
        
        for (int y = 0; y < 9; y++)
        {
            if (y == 4 && node.HasRightDoor)
                Raylib.DrawRectangle(offsetX + 14 * tileSize, offsetY + y * tileSize, tileSize - 1, tileSize - 1, Color.Beige);
            else
                Raylib.DrawRectangle(offsetX + 14 * tileSize, offsetY + y * tileSize, tileSize - 1, tileSize - 1, Color.DarkGray);
        }
        
        // Draw room interior
        int[,] layout = node.RoomData!.Layout;
        
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 13; x++)
            {
                int tile = layout[y, x];
                Color tileColor = Color.Gray;
                
                switch (tile)
                {
                    case 0: tileColor = Color.Beige; break;
                    case 2: tileColor = Color.Brown; break;
                    case 3: tileColor = Color.Gold; break;
                }
                
                Raylib.DrawRectangle(
                    offsetX + (x + 1) * tileSize,
                    offsetY + (y + 1) * tileSize,
                    tileSize - 1,
                    tileSize - 1,
                    tileColor
                );
            }
        }
        
        if (!isPaused)
        {
            string roomTypeText = $"Room: {node.Type}";
            Raylib.DrawText(roomTypeText, 10, 420, 16, Color.White);
        }
    }
}