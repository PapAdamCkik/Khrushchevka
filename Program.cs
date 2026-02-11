using Raylib_cs;

Raylib.InitWindow(800, 450, "Hello Raylib-cs");
Raylib.SetTargetFPS(60);

while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);
    Raylib.DrawText("Hello from Raylib-cs!", 190, 200, 20, Color.Black);
    Raylib.EndDrawing();
}

Raylib.CloseWindow();
