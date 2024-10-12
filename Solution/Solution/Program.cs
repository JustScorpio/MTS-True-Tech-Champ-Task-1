using Newtonsoft.Json;
using Solution;
using System.Diagnostics;
using System.Net.Http.Json;

var token = "9751045c-65f4-4949-8cf3-9361536d9a5512f09d25-7095-465c-8f74-4e9dd7919206";

var matrix = new int[16][];
for (int i = 0; i < 16; i++)
{
    matrix[i] = new int[16];
    for (int j = 0; j < 16; j++)
        matrix[i][j] = -1;
}

var step = 166.66;
var cellsCount = 256;

var cordX = 0;
var cordY = 15;
var direction = 0;

var client = new HttpClient(new HttpClientHandler());

# region attempt 1: scouting (P.S mouse continue exploring the maze even after it reached the center)
Console.WriteLine("Attempt #1 Started");

var watch = new Stopwatch();
watch.Start();

while (cellsCount > 0)
{
    //analysis
    var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-cells/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
    var sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);

    direction = (int)Math.Round(sensorData.rotation_yaw);
    if (direction == -180)
        direction *= -1;

    var isWallNorth = false;
    var isWallSouth = false;
    var isWallWest = false;
    var isWallEast = false;

    switch (direction)
    {
        case 0:
            isWallNorth = sensorData.front_distance < step / 2;
            isWallSouth = sensorData.back_distance < step / 2;
            isWallWest = sensorData.left_side_distance < step / 2;
            isWallEast = sensorData.right_side_distance < step / 2;
            break;
        case 90:
            isWallNorth = sensorData.left_side_distance < step / 2;
            isWallSouth = sensorData.right_side_distance < step / 2;
            isWallWest = sensorData.back_distance < step / 2;
            isWallEast = sensorData.front_distance < step / 2;
            break;
        case 180:
            isWallNorth = sensorData.back_distance < step / 2;
            isWallSouth = sensorData.front_distance < step / 2;
            isWallWest = sensorData.right_side_distance < step / 2;
            isWallEast = sensorData.left_side_distance < step / 2;
            break;
        case -90:
            isWallNorth = sensorData.right_side_distance < step / 2;
            isWallSouth = sensorData.left_side_distance < step / 2;
            isWallWest = sensorData.front_distance < step / 2;
            isWallEast = sensorData.back_distance < step / 2;
            break;
    }

    var cellTypeBinary = (isWallNorth ? 1 : 0) * 1 + (isWallEast ? 1 : 0) * 2 + (isWallSouth ? 1 : 0) * 4 + (isWallWest ? 1 : 0) * 8;
    var cellType = 0;

    switch (cellTypeBinary)
    {
        case 1:
            cellType = 2;
            break;
        case 2:
            cellType = 3;
            break;
        case 3:
            cellType = 7;
            break;
        case 4:
            cellType = 4;
            break;
        case 5:
            cellType = 10;
            break;
        case 6:
            cellType = 6;
            break;
        case 7:
            cellType = 11;
            break;
        case 8:
            cellType = 1;
            break;
        case 9:
            cellType = 8;
            break;
        case 10:
            cellType = 9;
            break;
        case 11:
            cellType = 12;
            break;
        case 12:
            cellType = 5;
            break;
        case 13:
            cellType = 13;
            break;
        case 14:
            cellType = 14;
            break;
        case 15:
            cellType = 15;
            break;
    }

    cordX = (int)Math.Floor(sensorData.down_y_offset / step + 8);
    cordY = (int)Math.Floor(sensorData.down_x_offset / step * -1 + 8);

    if (cordX < 0) cordX = 0;
    if (cordY < 0) cordY = 0;

    if (matrix[cordY][cordX] == -1)
    {
        matrix[cordY][cordX] = cellType;
        cellsCount -= 1;
        Console.WriteLine(string.Format($"Cell: x = {cordX}; y = {cordY} has type {cellType}. Cells to go: {cellsCount}"));
    }
    else
    {
        //Build up artificial walls to prevent mouse from looping away from the center of the maze
        switch (direction)
        {
            case 0:
                if (cellType == 1 && matrix[cordY - 1][cordX] > 0 && matrix[cordY][cordX + 1] == -1)
                    isWallNorth = true;
                else if (cellType == 2 && matrix[cordY][cordX - 1] > 0 && matrix[cordY][cordX + 1] == -1)
                    isWallWest = true;
                else if (cellType == 3 && matrix[cordY][cordX - 1] > 0 && matrix[cordY - 1][cordX] == -1)
                    isWallWest = true;
                else if (cellType == 0)
                    if (matrix[cordY][cordX - 1] > 0 && matrix[cordY - 1][cordX] > 0 && matrix[cordY][cordX + 1] == -1)
                    {
                        isWallWest = true;
                        isWallNorth = true;
                    }
                    else if (matrix[cordY][cordX - 1] > 0 && matrix[cordY - 1][cordX] == -1 && matrix[cordY][cordX + 1] > 0)
                    {
                        isWallWest = true;
                    }
                break;
            case 90:
                if (cellType == 2 && matrix[cordY][cordX + 1] > 0 && matrix[cordY + 1][cordX] == -1)
                    isWallEast = true;
                else if (cellType == 3 && matrix[cordY - 1][cordX] > 0 && matrix[cordY + 1][cordX] == -1)
                    isWallNorth = true;
                else if (cellType == 4 && matrix[cordY - 1][cordX] > 0 && matrix[cordY][cordX + 1] == -1)
                    isWallNorth = true;
                else if (cellType == 0)
                    if (matrix[cordY - 1][cordX] > 0 && matrix[cordY][cordX + 1] > 0 && matrix[cordY + 1][cordX] == -1)
                    {
                        isWallNorth = true;
                        isWallEast = true;
                    }
                    else if (matrix[cordY - 1][cordX] > 0 && matrix[cordY][cordX + 1] == -1 && matrix[cordY + 1][cordX] > 0)
                    {
                        isWallNorth = true;
                    }
                break;
            case 180:
                if (cellType == 3 && matrix[cordY + 1][cordX] > 0 && matrix[cordY][cordX - 1] == -1)
                    isWallSouth = true;
                else if (cellType == 4 && matrix[cordY][cordX + 1] > 0 && matrix[cordY][cordX - 1] == -1)
                    isWallEast = true;
                else if (cellType == 1 && matrix[cordY][cordX + 1] > 0 && matrix[cordY + 1][cordX] == -1)
                    isWallEast = true;
                else if (cellType == 0)
                    if (matrix[cordY][cordX + 1] > 0 && matrix[cordY + 1][cordX] > 0 && matrix[cordY][cordX - 1] == -1)
                    {
                        isWallEast = true;
                        isWallSouth = true;
                    }
                    else if (matrix[cordY][cordX + 1] > 0 && matrix[cordY + 1][cordX] == -1 && matrix[cordY][cordX - 1] > 0)
                    {
                        isWallEast = true;
                    }
                break;
            case -90:
                if (cellType == 4 && matrix[cordY][cordX - 1] > 0 && matrix[cordY - 1][cordX] == -1)
                    isWallWest = true;
                else if (cellType == 1 && matrix[cordY + 1][cordX] > 0 && matrix[cordY - 1][cordX] == -1)
                    isWallSouth = true;
                else if (cellType == 2 && matrix[cordY + 1][cordX] > 0 && matrix[cordY][cordX - 1] == -1)
                    isWallSouth = true;
                else if (cellType == 0)
                    if (matrix[cordY + 1][cordX] > 0 && matrix[cordY][cordX - 1] > 0 && matrix[cordY - 1][cordX] == -1)
                    {
                        isWallSouth = true;
                        isWallWest = true;
                    }
                    else if (matrix[cordY + 1][cordX] > 0 && matrix[cordY][cordX - 1] == -1 && matrix[cordY - 1][cordX] > 0)
                    {
                        isWallSouth = true;
                    }
                break;
        }
    }

    // move
    if (direction == 0 && !isWallWest || direction == 90 && !isWallNorth || direction == 180 && !isWallEast || direction == -90 && !isWallSouth)
    {
        await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);

        switch (direction)
        {
            case 0:
                direction = -90;
                break;
            case 90:
                direction = 0;
                break;
            case 180:
                direction = 90;
                break;
            case -90:
                direction = 180;
                break;
        }
    }
    else
    {
        while (direction == 0 && isWallNorth || direction == 90 && isWallEast || direction == 180 && isWallSouth || direction == -90 && isWallWest)
        {
            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);

            switch (direction)
            {
                case 0:
                    direction = 90;
                    break;
                case 90:
                    direction = 180;
                    break;
                case 180:
                    direction = -90;
                    break;
                case -90:
                    direction = 0;
                    break;
            }
        }
    }

    await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
}

//Calculate shortest path
var destDistMatrix = new int[16][];

for (int i = 0; i < 16; i++)
{
    destDistMatrix[i] = new int[16];
    for (int j = 0; j < 16; j++)
        destDistMatrix[i][j] = 256;
}

destDistMatrix[7][7] = 0;
destDistMatrix[7][8] = 0;
destDistMatrix[8][7] = 0;
destDistMatrix[8][8] = 0;

var cellTypesWithNoWallTop = new int[] { 0, 1, 3, 4, 5, 6, 9, 14 };
var cellTypesWithNoWallRight = new int[] { 0, 1, 2, 4, 5, 8, 10, 13 };
var cellTypesWithNoWallBottom = new int[] { 0, 1, 2, 3, 7, 8, 9, 12 };
var cellTypesWithNoWallLeft = new int[] { 0, 2, 3, 4, 6, 7, 10, 11 };

var curDistance = 0;
while (destDistMatrix[15][0] == 256)
{
    curDistance++;

    for (int y = 0; y < 16; y++)
    {
        for (int x = 0; x < 16; x++)
        {
            if (destDistMatrix[y][x] == curDistance - 1)
            {
                if (cellTypesWithNoWallTop.Contains(matrix[y][x]) && destDistMatrix[y - 1][x] == 256)
                    destDistMatrix[y - 1][x] = curDistance;
                if (cellTypesWithNoWallRight.Contains(matrix[y][x]) && destDistMatrix[y][x + 1] == 256)
                    destDistMatrix[y][x + 1] = curDistance;
                if (cellTypesWithNoWallBottom.Contains(matrix[y][x]) && destDistMatrix[y + 1][x] == 256)
                    destDistMatrix[y + 1][x] = curDistance;
                if (cellTypesWithNoWallLeft.Contains(matrix[y][x]) && destDistMatrix[y][x - 1] == 256)
                    destDistMatrix[y][x - 1] = curDistance;
            }
        }
    }
}

watch.Stop();
TimeSpan t = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
Console.WriteLine($"Attempt #1 Completed. Time: {string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds)}");
await client.PostAsync("http://127.0.0.1:8801/api/v1/maze/restart?token=" + token, null);
#endregion

#region attempt 2 go through the shortest cut and wait for each request to complete (slower but safier)
Console.WriteLine("Attempt #2 Started");

cordX = 0;
cordY = 15;
direction = 0;
curDistance = destDistMatrix[15][0];

watch.Restart();

while (true)
{
    //Not wasting time for retrieving data from sensors
    //var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-cells/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
    //var sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);

    //var cordX = (int)Math.Floor(sensorData.down_y_offset / step + 8);
    //var cordY = (int)Math.Floor(sensorData.down_x_offset / step * -1 + 8);
    //var direction = Math.Round(sensorData.rotation_yaw);
    //if (direction == -180)
    //    direction *= -1;

    Console.WriteLine("Distance to finish: " + destDistMatrix[cordY][cordX]);
    switch (direction)
    {
        case 0:
            if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                cordY--;
            }
            else if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = -90;
                cordX--;
            }
            else if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 90;
                cordX++;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case 90:
            if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                cordX++;
            }
            else if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 0;
                cordY--;
            }
            else if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 180;
                cordY++;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case 180:
            if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                cordY++;
            }
            else if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 90;
                cordX++;
            }
            else if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = -90;
                cordX--;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case -90:
            if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                cordX--;
            }
            else if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 180;
                cordY++;
            }
            else if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 0;
                cordY--;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
    }

    if (destDistMatrix[cordY][cordX] == 0)
        break;
}

watch.Stop();
t = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
Console.WriteLine($"Attempt #2 Completed. Time: {string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds)}");
await client.PostAsync("http://127.0.0.1:8801/api/v1/maze/restart?token=" + token, null);
#endregion

#region attempt 3 (without debug messages)
Console.WriteLine("Attempt #3 Started");



cordX = 0;
cordY = 15;
direction = 0;
curDistance = destDistMatrix[15][0];

watch.Restart();

while (true)
{
    switch (direction)
    {
        case 0:
            if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                cordY--;
            }
            else if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = -90;
                cordX--;
            }
            else if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 90;
                cordX++;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case 90:
            if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                cordX++;
            }
            else if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 0;
                cordY--;
            }
            else if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 180;
                cordY++;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case 180:
            if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                cordY++;
            }
            else if (cellTypesWithNoWallRight.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX + 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 90;
                cordX++;
            }
            else if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = -90;
                cordX--;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
        case -90:
            if (cellTypesWithNoWallLeft.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY][cordX - 1] < destDistMatrix[cordY][cordX])
            {
                cordX--;
            }
            else if (cellTypesWithNoWallBottom.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY + 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/left?token=" + token, null);
                direction = 180;
                cordY++;
            }
            else if (cellTypesWithNoWallTop.Contains(matrix[cordY][cordX]) && destDistMatrix[cordY - 1][cordX] < destDistMatrix[cordY][cordX])
            {
                await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/right?token=" + token, null);
                direction = 0;
                cordY--;
            }

            await client.PostAsync("http://127.0.0.1:8801/api/v1/robot-cells/forward?token=" + token, null);
            break;
    }

    if (destDistMatrix[cordY][cordX] == 0)
        break;
}

watch.Stop();
t = TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds);
Console.WriteLine($"Attempt #3 Completed. Time: {string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms", t.Hours, t.Minutes, t.Seconds, t.Milliseconds)}");
//await client.PostAsync("http://127.0.0.1:8801/api/v1/maze/restart?token=" + token, null);
#endregion


