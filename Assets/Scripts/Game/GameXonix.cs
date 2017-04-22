using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Game
{
    class GameXonix
    {
        #region CONSTANTS
        const int POINT_SIZE = 10;
        const int FIELD_WIDTH = 640 / POINT_SIZE;
        const int FIELD_HEIGHT = 430 / POINT_SIZE;
        public const int LEFT = 37;
        public const int UP = 38;
        public const int RIGHT = 39;
        public const int DOWN = 40;
        const int COLOR_TEMP = 1;
        const int COLOR_WATER = 0;
        const int COLOR_LAND = 0x00a8a8;
        const int COLOR_TRACK = 0x901290;
        const int PERCENT_OF_WATER_CAPTURE = 75;

        #endregion

        public static Xonix xonix;

        static Field field;
        static Balls balls;
        static Cubes cubes;
        public static GameOverAndPause gameOverOrPause;

        private Canvas canvas;

        public static Color GetColor(int color)
        {
            switch (color)
            {
                case COLOR_TEMP: return Color.white;
                case COLOR_WATER: return Color.black;
                case COLOR_LAND: return Color.cyan;
                case COLOR_TRACK: return Color.magenta;
            }
            return Color.red;
        }

        public static Color[] GetColors(int size, Color color)
        {
            var result = new Color[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = color;
            }
            return result;
        }

        public GameXonix()
        {
            xonix = new Xonix();
            field = new Field();
            balls = new Balls();
            cubes = new Cubes();
            gameOverOrPause = new GameOverAndPause();
            canvas = new Canvas();
    }

        public IEnumerator Go()
        {
            while (!gameOverOrPause.IsGameOver())
            {
                if (gameOverOrPause.IsPaused())
                    yield return new WaitForSeconds(0.5f);
                else
                {
                    xonix.Move();
                    balls.Move();
                    cubes.Move();
                    canvas.Paint();
                    yield return new WaitForSeconds(0.015f);
                    if(GameController.Instance.TimeIsUp)
                    {
                        GameController.Instance.SetTimer();
                        cubes.AddCube();
                    }
                    if (xonix.IsSelfCrosed() || balls.IsHitTrackOrXonix() || cubes.IsHitXonix())
                    {
                        GameController.Instance.SetTimer();
                        xonix.DecreaseLivesCount();
                        GameController.Instance.SetLivesCount(xonix.GetLivesCount());
                        if (xonix.GetLivesCount() > 0)
                        {
                            xonix.InitXonix();
                            field.ClearTrack();
                            canvas.Paint();
                            yield return new WaitForSeconds(2f);
                        }
                    }
                    if (field.GetCurrentPercent() >= PERCENT_OF_WATER_CAPTURE)
                    {
                        GameController.Instance.SetTimer();
                        xonix.LevelUp();
                        field.InitField();
                        xonix.InitXonix();
                        //cube.InitCube();
                        balls.AddBall();
                        canvas.Paint();
                        yield return new WaitForSeconds(1f);
                    }
                }

            }
        }

        class Field
        {
            private const int WATER_AREA = (FIELD_WIDTH - 4) * (FIELD_HEIGHT - 4);
            private int[,] _field = new int[FIELD_WIDTH, FIELD_HEIGHT];
            private float _currentWaterArea;
            //private int _scoreCount = 0;

            public Field()
            {
                InitField();
            }

            public void InitField()
            {
                for (int y = 0; y < FIELD_HEIGHT; y++)
                    for (int x = 0; x < FIELD_WIDTH; x++)
                        _field[x, y] = (x < 2 || x > FIELD_WIDTH - 3 || y < 2 || y > FIELD_HEIGHT - 3)
                            ? COLOR_LAND
                            : COLOR_WATER;
                _currentWaterArea = WATER_AREA;
            }

            public int GetCellColor(int x, int y)
            {
                if (x < 0 || y < 0 || x > FIELD_WIDTH - 1 || y > FIELD_HEIGHT - 1) return COLOR_WATER;
                return _field[x, y];
            }

            public void SetCellColor(int x, int y, int color)
            {
                _field[x, y] = color;
            }

            //int GetScoreCount()
            //{
            //    return _scoreCount;
            //}

            public int GetCurrentPercent()
            {
                return (int) Mathf.Round(100f - _currentWaterArea / WATER_AREA * 100);
            }

            public void ClearTrack()
            {
                for (int y = 0; y < FIELD_HEIGHT; y++)
                    for (int x = 0; x < FIELD_WIDTH; x++)
                        if (_field[x, y] == COLOR_TRACK) _field[x, y] = COLOR_WATER;
            }

            void FillTemporary(int x, int y)
            {
                var ballSqгare = 0;
                if (_field[x, y] > COLOR_WATER) return;
                _field[x, y] = COLOR_TEMP; // filling temporary color
                for (int dx = -1; dx < 2; dx++)
                    for (int dy = -1; dy < 2; dy++)
                    {
                        FillTemporary(x + dx, y + dy);
                    }
            }

            public void TryToFill()
            {
                _currentWaterArea = 0;

                foreach (var ball in balls.GetBalls())
                {
                    FillTemporary(ball.GetX(), ball.GetY());
                }
                for (int y = 0; y < FIELD_HEIGHT; y++)
                    for (int x = 0; x < FIELD_WIDTH; x++)
                    {
                        if (_field[x, y] == COLOR_TRACK || _field[x, y] == COLOR_WATER)
                        {
                            _field[x, y] = COLOR_LAND;
                            //_scoreCount += 10;
                        }
                        if (_field[x, y] == COLOR_TEMP)
                        {
                            _field[x, y] = COLOR_WATER;
                            _currentWaterArea++;
                        }
                    }
            }

            public void Paint()
            {
                for (int y = 0; y < FIELD_HEIGHT; y++)
                    for (int x = 0; x < FIELD_WIDTH; x++)
                    {
                        GameController.Instance.Texture.SetPixels(x * POINT_SIZE, y * POINT_SIZE, POINT_SIZE, POINT_SIZE, GetColors(POINT_SIZE * POINT_SIZE, GetColor(_field[x, y])));
                    }
            }
        }

        internal class Xonix
        {
            private int _x, _y, _direction, _livesCount = 3, _level = 1;
            private bool _isWater, _isSelfCross;

            public Xonix()
            {
                InitXonix();
            }

            public void InitXonix()
            {
                _y = 0;
                _x = FIELD_WIDTH / 2;
                _direction = 0;
                _isWater = false;
            }

            public int GetX()
            {
                return _x;
            }

            public int GetY()
            {
                return _y;
            }

            public int GetLivesCount()
            {
                return _livesCount;
            }

            public void DecreaseLivesCount()
            {
                _livesCount--;
            }

            public int GetCurrentLevel()
            {
                return _level;
            }

            public void LevelUp()
            {
                _level++;
            }

            public void SetDirection(int direction)
            {
                this._direction = direction;
            }

            public void Move()
            {
                if (_direction == LEFT) _x--;
                if (_direction == RIGHT) _x++;
                if (_direction == UP) _y--;
                if (_direction == DOWN) _y++;
                if (_x < 0) _x = 0;
                if (_y < 0) _y = 0;
                if (_y > FIELD_HEIGHT - 1) _y = FIELD_HEIGHT - 1;
                if (_x > FIELD_WIDTH - 1) _x = FIELD_WIDTH - 1;
                _isSelfCross = field.GetCellColor(_x, _y) == COLOR_TRACK;
                if (field.GetCellColor(_x, _y) == COLOR_LAND && _isWater)
                {
                    _direction = 0;
                    _isWater = false;
                    field.TryToFill();
                }
                if (field.GetCellColor(_x, _y) == COLOR_WATER)
                {
                    _isWater = true;
                    field.SetCellColor(_x, _y, COLOR_TRACK);
                }
            }

            public bool IsSelfCrosed()
            {
                return _isSelfCross;
            }

            public void Paint()
            {
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE, _y * POINT_SIZE, POINT_SIZE, POINT_SIZE, GetColors(POINT_SIZE * POINT_SIZE, (field.GetCellColor(_x, _y) == COLOR_LAND) ? GetColor(COLOR_TRACK) : Color.white));
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE + 3, _y * POINT_SIZE + 3, POINT_SIZE - 6, POINT_SIZE - 6, GetColors((POINT_SIZE - 6) * (POINT_SIZE - 6), (field.GetCellColor(_x, _y) == COLOR_LAND) ? Color.white : GetColor(COLOR_TRACK)));
            }
        }

        class Balls
        {
            private List<Ball> _balls = new List<Ball>();

            public Balls()
            {
                AddBall();
            }

            public void AddBall()
            {
                _balls.Add(new Ball());
            }

            public void Move()
            {
                foreach (var ball in _balls)
                {
                    ball.Move();
                }
            }

            public List<Ball> GetBalls()
            {
                return _balls;
            }

            public bool IsHitTrackOrXonix()
            {
                foreach (var ball in _balls)
                    if (ball.IsHitTrackOrXonix())
                        return true;
                return false;
            }

            public void Paint()
            {
                foreach (var ball in _balls)
                    ball.Paint();
            }
        }

        class Ball
        {
            private int _x, _y, _dx, _dy;

            public Ball()
            {
                do
                {
                    _x = UnityEngine.Random.Range(0, FIELD_WIDTH);
                    _y = UnityEngine.Random.Range(0, FIELD_HEIGHT);
                }
                while (field.GetCellColor(_x, _y) > COLOR_WATER);

                _dx = UnityEngine.Random.Range(0, 1) == 0 ? 1 : -1;
                _dy = UnityEngine.Random.Range(0, 1) == 0 ? 1 : -1;
            }

            void UpdateDxAndDy()
            {
                if (field.GetCellColor(_x + _dx, _y) == COLOR_LAND)
                    _dx = -_dx;
                if (field.GetCellColor(_x, _y + _dy) == COLOR_LAND)
                    _dy = -_dy;
            }

            public void Move()
            {
                UpdateDxAndDy();
                _x += _dx;
                _y += _dy;
            }

            public int GetX()
            {
                return _x;
            }

            public int GetY()
            {
                return _y;
            }

            public int GetSquare()
            {
                return _x * _y;
            }

            public bool IsHitTrackOrXonix()
            {
                UpdateDxAndDy();
                if (field.GetCellColor(_x + _dx, _y + _dy) == COLOR_TRACK)
                    return true;
                if (_x + _dx == xonix.GetX() && _y + _dy == xonix.GetY())
                    return true;
                return false;
            }

            public void Paint()
            {
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE, _y * POINT_SIZE, POINT_SIZE, POINT_SIZE, GetColors(POINT_SIZE * POINT_SIZE, Color.white));
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE + 2, _y * POINT_SIZE + 2, POINT_SIZE - 4, POINT_SIZE - 4, GetColors((POINT_SIZE - 4) * (POINT_SIZE - 4), GetColor(COLOR_LAND)));
            }
        }

        class Cubes
        {
            private List<Cube> _cubes = new List<Cube>();

            public Cubes()
            {
                AddCube();
            }

            public void AddCube()
            {
                _cubes.Add(new Cube());
            }

            public void Move()
            {
                foreach (var cube in _cubes)
                {
                    cube.Move();
                }
            }

            public List<Cube> GetCubes()
            {
                return _cubes;
            }

            public bool IsHitXonix()
            {
                foreach (var cube in _cubes)
                    if (cube.IsHitXonix())
                        return true;
                return false;
            }

            public void Paint()
            {
                foreach (var cube in _cubes)
                    cube.Paint();
            }
        }

        class Cube
        {
            private int _x, _y, _dx, _dy;

            public Cube()
            {
                InitCube();
            }

            public void InitCube()
            {
                _x = _y = _dx = 1;
                _dy = -1;
            }

            void UpdateDxAndDy()
            {
                if (field.GetCellColor(_x + _dx, _y) == COLOR_WATER)
                    _dx = -_dx;
                if (field.GetCellColor(_x, _y + _dy) == COLOR_WATER)
                    _dy = -_dy;
            }

            public void Move()
            {
                UpdateDxAndDy();
                _x += _dx;
                _y += _dy;
            }

            public bool IsHitXonix()
            {
                UpdateDxAndDy();
                if (_x + _dx == xonix.GetX() && _y + _dy == xonix.GetY())
                    return true;
                return false;
            }

            public void Paint()
            {
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE, _y * POINT_SIZE, POINT_SIZE, POINT_SIZE, GetColors(POINT_SIZE * POINT_SIZE, GetColor(COLOR_WATER)));
                GameController.Instance.Texture.SetPixels(_x * POINT_SIZE + 2, _y * POINT_SIZE + 2, POINT_SIZE - 4, POINT_SIZE - 4, GetColors((POINT_SIZE - 4) * (POINT_SIZE - 4), GetColor(COLOR_LAND)));
            }
        }

        internal class GameOverAndPause
        {
            private bool _gameOver;
            private bool _isPaused;

            public bool IsGameOver()
            {
                return _gameOver;
            }

            public bool IsPaused()
            {
                return _isPaused;
            }

            public void OnGameOver()
            {
                if (xonix.GetLivesCount() == 0)
                {
                    GameController.Instance.SetUI(false);
                    _gameOver = true;
                }
            }

            public void OnPause()
            {
                _isPaused = !_isPaused;
            }
        }

        class Canvas
        {
            public void Paint()
            {                
                field.Paint();
                GameController.Instance.SetFillAmount(field.GetCurrentPercent());
                GameController.Instance.SetLevelNum(xonix.GetCurrentLevel());
                xonix.Paint();
                balls.Paint();
                cubes.Paint();
                gameOverOrPause.OnGameOver();
                GameController.Instance.Texture.Apply();
            }
        }
    }
}
