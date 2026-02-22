using System;
using System.Collections.Generic;
using System.Linq;

namespace Khrushchevka_RPG;

public class GridPosition
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is GridPosition other)
            return X == other.X && Y == other.Y;
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

public class FloorNode
{
    public GridPosition Position { get; set; }
    public Room? RoomData { get; set; }
    public RoomType Type { get; set; }
    
    // Door connections (calculated based on neighboring rooms)
    public bool HasTopDoor { get; set; }
    public bool HasRightDoor { get; set; }
    public bool HasBottomDoor { get; set; }
    public bool HasLeftDoor { get; set; }
    
    public FloorNode(GridPosition pos, RoomType type = RoomType.Normal)
    {
        Position = pos;
        Type = type;
        RoomData = null;
        HasTopDoor = false;
        HasRightDoor = false;
        HasBottomDoor = false;
        HasLeftDoor = false;
    }
}

public class FloorGenerator
{
    private Random rand;
    private Dictionary<GridPosition, FloorNode> floor;
    private int currentFloorNumber;
    
    public FloorGenerator()
    {
        rand = new Random();
        floor = new Dictionary<GridPosition, FloorNode>();
        currentFloorNumber = 1;
    }
    
    // Get min/max rooms for a specific floor
    private (int min, int max) GetRoomLimits(int floorNumber)
    {
        return floorNumber switch
        {
            1 => (6, 10),
            2 => (6, 10),
            3 => (8, 15),
            4 => (8, 15),
            5 => (12, 20),
            6 => (15, 25),
            7 => (15, 25),
            8 => (12, 20),  // Unlock required
            9 => (12, 20),  // Unlock required
            _ => (6, 10)    // Default
        };
    }
    
    public Dictionary<GridPosition, FloorNode> GenerateFloor(int floorNumber)
    {
        currentFloorNumber = floorNumber;
        floor.Clear();
        
        // Get room limits for this floor
        var (minRooms, maxRooms) = GetRoomLimits(floorNumber);
        
        // Start room at (0, 0) - ALWAYS empty room with potential for 4 doors
        GridPosition startPos = new GridPosition(0, 0);
        floor[startPos] = new FloorNode(startPos, RoomType.Start);
        
        // Generate main path
        int targetRooms = rand.Next(minRooms, maxRooms + 1);
        List<GridPosition> frontier = new List<GridPosition> { startPos };
        
        while (floor.Count < targetRooms && frontier.Count > 0)
        {
            // Pick random position from frontier
            GridPosition current = frontier[rand.Next(frontier.Count)];
            frontier.Remove(current);
            
            // Try to add rooms in all directions
            List<GridPosition> directions = new List<GridPosition>
            {
                new GridPosition(current.X, current.Y - 1),  // Top
                new GridPosition(current.X + 1, current.Y),  // Right
                new GridPosition(current.X, current.Y + 1),  // Bottom
                new GridPosition(current.X - 1, current.Y)   // Left
            };
            
            // Shuffle directions
            for (int i = directions.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                var temp = directions[i];
                directions[i] = directions[j];
                directions[j] = temp;
            }
            
            foreach (var newPos in directions)
            {
                if (!floor.ContainsKey(newPos) && rand.NextDouble() < 0.5)
                {
                    floor[newPos] = new FloorNode(newPos);
                    frontier.Add(newPos);
                    
                    if (floor.Count >= targetRooms)
                        break;
                }
            }
        }
        
        // Place special rooms (Item, Shop, Boss)
        PlaceSpecialRooms();
        
        // Calculate door connections based on neighboring rooms
        CalculateDoorConnections();
        
        // Assign room blueprints
        AssignRoomBlueprints();
        
        return floor;
    }
    
    private void PlaceSpecialRooms()
    {
        // Find all rooms with only one connection (dead ends)
        List<FloorNode> deadEnds = new List<FloorNode>();
        
        foreach (var node in floor.Values)
        {
            if (node.Type == RoomType.Start)
                continue;
                
            int connections = CountConnections(node.Position);
            if (connections == 1)
                deadEnds.Add(node);
        }
        
        // Need at least 3 dead ends for special rooms
        if (deadEnds.Count < 3)
        {
            // Create more dead ends if needed
            while (deadEnds.Count < 3)
            {
                // Add a random branch
                var randomRoom = floor.Values.ToArray()[rand.Next(floor.Count)];
                List<GridPosition> directions = new List<GridPosition>
                {
                    new GridPosition(randomRoom.Position.X, randomRoom.Position.Y - 1),
                    new GridPosition(randomRoom.Position.X + 1, randomRoom.Position.Y),
                    new GridPosition(randomRoom.Position.X, randomRoom.Position.Y + 1),
                    new GridPosition(randomRoom.Position.X - 1, randomRoom.Position.Y)
                };
                
                foreach (var dir in directions)
                {
                    if (!floor.ContainsKey(dir))
                    {
                        var newNode = new FloorNode(dir);
                        floor[dir] = newNode;
                        deadEnds.Add(newNode);
                        break;
                    }
                }
            }
        }
        
        // Shuffle and assign special room types
        for (int i = deadEnds.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            var temp = deadEnds[i];
            deadEnds[i] = deadEnds[j];
            deadEnds[j] = temp;
        }
        
        deadEnds[0].Type = RoomType.Item;
        deadEnds[1].Type = RoomType.Shop;
        deadEnds[2].Type = RoomType.Boss;
    }
    
    private int CountConnections(GridPosition pos)
    {
        int count = 0;
        
        if (floor.ContainsKey(new GridPosition(pos.X, pos.Y - 1))) count++;  // Top
        if (floor.ContainsKey(new GridPosition(pos.X + 1, pos.Y))) count++;  // Right
        if (floor.ContainsKey(new GridPosition(pos.X, pos.Y + 1))) count++;  // Bottom
        if (floor.ContainsKey(new GridPosition(pos.X - 1, pos.Y))) count++;  // Left
        
        return count;
    }
    
    private void CalculateDoorConnections()
    {
        foreach (var node in floor.Values)
        {
            node.HasTopDoor = floor.ContainsKey(new GridPosition(node.Position.X, node.Position.Y - 1));
            node.HasRightDoor = floor.ContainsKey(new GridPosition(node.Position.X + 1, node.Position.Y));
            node.HasBottomDoor = floor.ContainsKey(new GridPosition(node.Position.X, node.Position.Y + 1));
            node.HasLeftDoor = floor.ContainsKey(new GridPosition(node.Position.X - 1, node.Position.Y));
        }
    }
    
    private void AssignRoomBlueprints()
    {
        foreach (var node in floor.Values)
        {
            // Assign appropriate room blueprint
            if (node.Type == RoomType.Start)
            {
                // Start room is ALWAYS empty (supports all 4 door directions)
                node.RoomData = RoomBlueprints.GetEmptyRoom();
            }
            else if (node.Type == RoomType.Normal)
            {
                // Get a room compatible with required doors
                node.RoomData = RoomBlueprints.GetNormalRoom(
                    node.HasTopDoor, 
                    node.HasRightDoor, 
                    node.HasBottomDoor, 
                    node.HasLeftDoor
                );
            }
            else  // Special rooms (Item, Shop, Boss)
            {
                node.RoomData = RoomBlueprints.GetSpecialRoom(node.Type);
            }
        }
    }
}