using System;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public enum RoomType
{
    Normal,
    Start,
    Item,
    Boss
}

public class Room
{
    public int[,] Layout { get; set; }
    public RoomType Type { get; set; }
    
    public bool TopBlocked { get; set; }
    public bool RightBlocked { get; set; }
    public bool BottomBlocked { get; set; }
    public bool LeftBlocked { get; set; }
    
    public Room(int[,] layout, RoomType type = RoomType.Normal)
    {
        Layout = layout;
        Type = type;
        
        TopBlocked = CheckTopBlocked(layout);
        RightBlocked = CheckRightBlocked(layout);
        BottomBlocked = CheckBottomBlocked(layout);
        LeftBlocked = CheckLeftBlocked(layout);
    }
    
    private bool CheckTopBlocked(int[,] layout)
    {
        return layout[0, 6] != 0;
    }
    
    private bool CheckBottomBlocked(int[,] layout)
    {
        return layout[6, 6] != 0;
    }
    
    private bool CheckLeftBlocked(int[,] layout)
    {
        return layout[3, 0] != 0;
    }
    
    private bool CheckRightBlocked(int[,] layout)
    {
        return layout[3, 12] != 0;
    }
    
    public bool IsCompatible(bool needTop, bool needRight, bool needBottom, bool needLeft)
    {
        if (needTop && TopBlocked) return false;
        if (needRight && RightBlocked) return false;
        if (needBottom && BottomBlocked) return false;
        if (needLeft && LeftBlocked) return false;
        
        return true;
    }
}

public static class RoomBlueprints
{
    public static List<Room> NormalRooms = new List<Room>();
    public static List<Room> SpecialRooms = new List<Room>();
    private static Room emptyRoom = null!;
    
    public static void Initialize()
    {
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
        
        int[,] horizontalObstacles = new int[7, 13]
        {
            {0,0,0,0,0,0,2,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,2,2,2,2,2,2,2,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,2,2,2,2,2,2,2,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,2,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(horizontalObstacles));
        
        int[,] verticalObstacles = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {2,0,2,0,0,0,2,0,0,0,2,0,2},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,2,0,0,0,2,0,0,0,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(verticalObstacles));
        
        int[,] ring = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,2,2,2,2,2,2,2,2,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,2,0,0,0,0,0,0,0,2,0,0},
            {0,0,2,2,2,2,2,2,2,2,2,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        NormalRooms.Add(new Room(ring));
        
        // SPECIAL ROOMS - Item and Boss only
        int[,] itemRoom = new int[7, 13]
        {
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,3,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0},
            {0,0,0,0,0,0,0,0,0,0,0,0,0}
        };
        SpecialRooms.Add(new Room(itemRoom, RoomType.Item));
        
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
    
    public static Room GetEmptyRoom()
    {
        return emptyRoom;
    }
    
    public static Room GetNormalRoom(bool needTop, bool needRight, bool needBottom, bool needLeft)
    {
        Random rand = new Random();
        
        List<Room> compatibleRooms = new List<Room>();
        foreach (var room in NormalRooms)
        {
            if (room.IsCompatible(needTop, needRight, needBottom, needLeft))
            {
                compatibleRooms.Add(room);
            }
        }
        
        if (compatibleRooms.Count > 0)
        {
            return compatibleRooms[rand.Next(compatibleRooms.Count)];
        }
        
        return emptyRoom;
    }
    
    public static Room GetSpecialRoom(RoomType type)
    {
        Random rand = new Random();
        List<Room> matchingRooms = SpecialRooms.FindAll(r => r.Type == type);
        
        if (matchingRooms.Count > 0)
            return matchingRooms[rand.Next(matchingRooms.Count)];
        
        return SpecialRooms[rand.Next(SpecialRooms.Count)];
    }
}