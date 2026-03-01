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
            _ => (6, 10)
        };
    }
    
    public Dictionary<GridPosition, FloorNode> GenerateFloor(int floorNumber)
    {
        currentFloorNumber = floorNumber;
        floor.Clear();
        
        var (minRooms, maxRooms) = GetRoomLimits(floorNumber);
        
        GridPosition startPos = new GridPosition(0, 0);
        floor[startPos] = new FloorNode(startPos, RoomType.Start);
        
        int targetRooms = rand.Next(minRooms, maxRooms + 1);
        List<GridPosition> frontier = new List<GridPosition> { startPos };
        
        while (floor.Count < targetRooms && frontier.Count > 0)
        {
            GridPosition current = frontier[rand.Next(frontier.Count)];
            frontier.Remove(current);
            
            List<GridPosition> directions = new List<GridPosition>
            {
                new GridPosition(current.X, current.Y - 1),
                new GridPosition(current.X + 1, current.Y),
                new GridPosition(current.X, current.Y + 1),
                new GridPosition(current.X - 1, current.Y)
            };
            
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
        
        PlaceSpecialRooms();
        CalculateDoorConnections();
        AssignRoomBlueprints();
        
        return floor;
    }
    
    private void PlaceSpecialRooms()
    {
        List<FloorNode> deadEnds = new List<FloorNode>();
        
        foreach (var node in floor.Values)
        {
            if (node.Type == RoomType.Start)
                continue;
                
            int connections = CountConnections(node.Position);
            if (connections == 1)
                deadEnds.Add(node);
        }
        
        if (deadEnds.Count < 2)
        {
            while (deadEnds.Count < 2)
            {
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
        
        for (int i = deadEnds.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            var temp = deadEnds[i];
            deadEnds[i] = deadEnds[j];
            deadEnds[j] = temp;
        }
        
        deadEnds[0].Type = RoomType.Item;
        deadEnds[1].Type = RoomType.Boss;
    }
    
    private int CountConnections(GridPosition pos)
    {
        int count = 0;
        
        if (floor.ContainsKey(new GridPosition(pos.X, pos.Y - 1))) count++;
        if (floor.ContainsKey(new GridPosition(pos.X + 1, pos.Y))) count++;
        if (floor.ContainsKey(new GridPosition(pos.X, pos.Y + 1))) count++;
        if (floor.ContainsKey(new GridPosition(pos.X - 1, pos.Y))) count++;
        
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
            if (node.Type == RoomType.Start)
            {
                node.RoomData = RoomBlueprints.GetEmptyRoom();
            }
            else if (node.Type == RoomType.Normal)
            {
                node.RoomData = RoomBlueprints.GetNormalRoom(
                    node.HasTopDoor, 
                    node.HasRightDoor, 
                    node.HasBottomDoor, 
                    node.HasLeftDoor
                );
            }
            else
            {
                node.RoomData = RoomBlueprints.GetSpecialRoom(node.Type);
            }
        }
    }
}