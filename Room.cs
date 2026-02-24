using System;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public enum RoomType
{
    Normal,
    Start,
    Item,
    Shop,
    Boss
}

public class Room
{
    public int[,] Layout { get; set; }  // 7x13 matrix for room interior (NO walls)
    public RoomType Type { get; set; }
    
    // Which door positions are BLOCKED by obstacles in this layout
    public bool TopBlocked { get; set; }
    public bool RightBlocked { get; set; }
    public bool BottomBlocked { get; set; }
    public bool LeftBlocked { get; set; }
    
    public Room(int[,] layout, RoomType type = RoomType.Normal)
    {
        Layout = layout;
        Type = type;
        
        // Check which door positions have obstacles
        TopBlocked = CheckTopBlocked(layout);
        RightBlocked = CheckRightBlocked(layout);
        BottomBlocked = CheckBottomBlocked(layout);
        LeftBlocked = CheckLeftBlocked(layout);
    }
    
    private bool CheckTopBlocked(int[,] layout)
    {
        // Check if there's an obstacle at door position (top middle: x=6, y=0)
        return layout[0, 6] != 0;
    }
    
    private bool CheckBottomBlocked(int[,] layout)
    {
        // Check if there's an obstacle at door position (bottom middle: x=6, y=6)
        return layout[6, 6] != 0;
    }
    
    private bool CheckLeftBlocked(int[,] layout)
    {
        // Check if there's an obstacle at door position (left middle: x=0, y=3)
        return layout[3, 0] != 0;
    }
    
    private bool CheckRightBlocked(int[,] layout)
    {
        // Check if there's an obstacle at door position (right middle: x=12, y=3)
        return layout[3, 12] != 0;
    }
    
    // Check if this room is compatible with required door configuration
    public bool IsCompatible(bool needTop, bool needRight, bool needBottom, bool needLeft)
    {
        // If we need a door but the position is blocked, not compatible
        if (needTop && TopBlocked) return false;
        if (needRight && RightBlocked) return false;
        if (needBottom && BottomBlocked) return false;
        if (needLeft && LeftBlocked) return false;
        
        return true;
    }
}

// Room blueprint storage
public static class RoomBlueprints
{
    public static List<Room> NormalRooms = new List<Room>();
    public static List<Room> SpecialRooms = new List<Room>();
    private static Room emptyRoom = null!;
    
    public static void Initialize()
    {
        // Room layouts - 0 = floor, 2 = obstacle/rock, 3 = special item, etc.
        // NO WALLS - just the interior content
        // IMPORTANT: Don't place obstacles at door positions: 
        //   Top: [0, 6]  Bottom: [6, 6]  Left: [3, 0]  Right: [3, 12]
        
        // NORMAL ROOMS
        
        // Simple empty room (compatible with all door configs)
        int[,] emptyRoomLayout = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        emptyRoom = new Room(emptyRoomLayout);
        NormalRooms.Add(emptyRoom);
        
        // Room with obstacles in corners (compatible with all door configs)
        int[,] cornerObstacles = new int[7, 13]
        {
            {2,2,0,0,0,0,0,0,0,0,0,2,2},
            {2,0,0,0,0,0,0,0,0,0,0,0,2},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {2,0,0,0,0,0,0,0,0,0,0,0,2},
            {2,2,0,0,0,0,0,0,0,0,0,2,2}
        };
        NormalRooms.Add(new Room(cornerObstacles));
        
        // Room with center obstacle (compatible with all door configs)
        int[,] centerObstacle = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,2,2,2,0,0,0,0,0},
            {0,0,0,0,0,2,2,2,0,0,0,0,0},
            {0,0,0,0,0,2,2,2,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(centerObstacle));
        
        // Room with scattered rocks (avoid door positions)
        int[,] scatteredRocks = new int[7, 13]
        {
            {0,0,2,0,0,0,0,0,0,2,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {2,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,2},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,0,0,0,2,0,0,0}
        };
        NormalRooms.Add(new Room(scatteredRocks));
        
        // Room with pillars (avoid door positions)
        int[,] pillars = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(pillars));
        
        // Room with horizontal obstacles (blocks top and bottom)
        int[,] horizontalObstacles = new int[7, 13]
        {
            {0,0,0,0,0,0,2,0,0,0,0,0,0},  // Top door blocked
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,2,2,2,2,2,2,2,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,2,2,2,2,2,2,2,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,2,0,0,0,0,0,0}   // Bottom door blocked
        };
        NormalRooms.Add(new Room(horizontalObstacles));  // Can only have left/right doors
        
        // Room with vertical obstacles (blocks left and right)
        int[,] verticalObstacles = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {2,0,2,0,0,0,2,0,0,0,2,0,2},  // Left and right doors blocked
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(verticalObstacles));  // Can only have top/bottom doors
        
        // Room with ring pattern (compatible with all doors)
        int[,] ring = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,2,2,2,2,2,2,2,2,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,2,2,2,2,2,2,2,2,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(ring));
        
        // SPECIAL ROOMS (Item, Shop, Boss)
        
        // Item room - pedestal in center (compatible with all doors)
        int[,] itemRoom = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,0,0,0,0,0,0},  // 3 = item pedestal
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        SpecialRooms.Add(new Room(itemRoom, RoomType.Item));
        
        // Shop room - items in corners (compatible with all doors)
        int[,] shopRoom = new int[7, 13]
        {
            {0,0,3,0,0,0,0,0,0,0,3,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,3,0,0,0,0,0,0,0,3,0,0}
        };
        SpecialRooms.Add(new Room(shopRoom, RoomType.Shop));
        
        // Boss room - large open space (compatible with all doors)
        int[,] bossRoom = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        SpecialRooms.Add(new Room(bossRoom, RoomType.Boss));
    }
    
    // Get the empty room (for start)
    public static Room GetEmptyRoom()
    {
        return emptyRoom;
    }
    
    // Get a random normal room that's compatible with door requirements
    public static Room GetNormalRoom(bool needTop, bool needRight, bool needBottom, bool needLeft)
    {
        Random rand = new Random();
        
        // Find all compatible rooms
        List<Room> compatibleRooms = new List<Room>();
        foreach (var room in NormalRooms)
        {
            if (room.IsCompatible(needTop, needRight, needBottom, needLeft))
            {
                compatibleRooms.Add(room);
            }
        }
        
        // If we found compatible rooms, pick one randomly
        if (compatibleRooms.Count > 0)
        {
            return compatibleRooms[rand.Next(compatibleRooms.Count)];
        }
        
        // Fallback: return empty room (always compatible)
        return emptyRoom;
    }
    
    // Get a special room by type (always compatible since they're open)
    public static Room GetSpecialRoom(RoomType type)
    {
        Random rand = new Random();
        List<Room> matchingRooms = SpecialRooms.FindAll(r => r.Type == type);
        
        if (matchingRooms.Count > 0)
            return matchingRooms[rand.Next(matchingRooms.Count)];
        
        // Fallback: return any special room
        return SpecialRooms[rand.Next(SpecialRooms.Count)];
    }
}