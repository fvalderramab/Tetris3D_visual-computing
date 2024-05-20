using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
// Este script se le aplica al GameObject Tetracube, en el que se van guardando una ficha hasta que cae (es el objeto padre entonces las instrucciones aplican a los hijos)
// de ahí en adelante se dejan como GameObjects estáticos aparte (no les afectan más las instrucciones del script)
public class GameBehaviourScript : MonoBehaviour
{
    public GameObject[] tetracubes; // Arreglo de objetos en los que se meten las 7 diferentes fichas (tetracubos)
    public GameObject[] rows; // Arreglo de objetos en los que se meten las filas para guardar los cubos que forman un tetracubo una vez este se vuelve estático
    private GameObject actualTetro; // Instancia de uno de los 7 tetracubos y es el que se puede controlar en el juego
    private GameObject ghostTetro; // Instancia de actualTetro pero se baja hasta colisionar

    private int width = 4;
    private int height = 11;
    private int depth = 4;
    private int[,,] grid; // Matriz en la que se guarda la posición de todos los cubos (sin cubo=0; activos=1; estáticos=2; ghost=3)
    private int score = 0;
    private int level = 0;
    private int tetroIndex;
    private int tetroIndex1;
    private int[] cubesInRow; // Almacena los cubos estáticos de cada fila
    private float interval = 3f; // Entre menos valor, más velocidad tiene el juego
    private float timer = 0.0f;

    private bool touching = false; // Detectar colisiones abajo de la ficha activa
    private bool ground = false; // Detectar colision abajo de la ficha ghost
    private bool canmoveX1 = true;
    private bool canmoveX0 = true;
    private bool canmoveZ1 = true;
    private bool canmoveZ0 = true;
    private bool a = true;
    private bool b = true;
    private bool pause = false;

    // Relativo a UI 
    public TMP_Text scoreText;
    public TMP_Text levelText;
    public TMP_Text gameOverText;
    public Button retry;
    public Image nextTetro;
    public AudioSource musicSource;
    private Material Ghost_material;

    void Start() 
    {
        // Para hacer que vaya a "targetFrameRate" fps
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        // Creación de la matriz que guarda posiciones grid (se pone +4 en y para poder hacer rotaciones cuando spawnea una pieza)
        grid = new int[width, height+4, depth];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height+4; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    grid[x, y, z] = 0;
                }
            }
        }

        // Inicialización de variables
        cubesInRow = new int[height];
        scoreText.text = "Score: " + score.ToString();
        levelText.text = "Level: 0";
        gameOverText.gameObject.SetActive(false);
        retry.gameObject.SetActive(false);
        musicSource = GetComponent<AudioSource>();
        Ghost_material = Resources.Load<Material>("Materials/Ghost_material");

        // Spawn de la primera ficha
        tetroIndex = UnityEngine.Random.Range(0, tetracubes.Length); // Elige aleatoriamente una de las fichas del arreglo
        tetroIndex1 = UnityEngine.Random.Range(0, tetracubes.Length); // Elige la ficha que siga
        nextTetro.sprite = Resources.Load<Sprite>(tetroIndex1.ToString() + "_prefab"); // Se muestra la ficha que sigue
        actualTetro = Instantiate(tetracubes[tetroIndex], transform.position, Quaternion.identity, transform); // Crea una copia de la ficha seleccionada y le asigna posición y rotación del GameObject Tetracube (y establece este como objeto padre)
        actualTetro.transform.position = new Vector3(width / 2 - 2, height - 1, depth / 2); // Se sincroniza la posición con la que se empieza con la posición del GameObject actualTetro
        cubePositions(1);
        updateGhost();
    }

    void Update() 
    {
        if (pause == false)
        {
            gameFlow(); // A menos que se pierda (se pausa), la ficha activa va a seguir bajando, actualizando grid y se va a seguir detectando suelo o fichas abajo
        }
    }

    private void gameFlow()
    {
        detection();
        timer += 0.03f;
        if (timer >= interval && actualTetro.transform.position.y > 0 && touching == false) // Cada cierto intervalo de tiempo, la ficha activa baja un cubo en Y, entonces se actualiza su posición en grid
        {
            actualTetro.transform.position = new Vector3(actualTetro.transform.position.x, actualTetro.transform.position.y - 1, actualTetro.transform.position.z);
            timer = 0.0f;
            updateGrid(1, 0);
            cubePositions(1);
        }
        if (actualTetro.transform.position.y == 0 || touching == true) // Si se detecta a la ficha activa tocando por encima algún cubo estático o el piso, la vuelve cubos estáticos y spawnea una nueva ficha
        {
            touching = false;
            timer = 0.0f;
            updateGrid(1, 2);
            convertIntoCubes();
            eliminateRow();
            tetroIndex = tetroIndex1;
            tetroIndex1 = UnityEngine.Random.Range(0, tetracubes.Length);
            nextTetro.sprite = Resources.Load<Sprite>(tetroIndex1.ToString() + "_prefab");
            actualTetro = Instantiate(tetracubes[tetroIndex], transform.position, Quaternion.identity, transform);
            actualTetro.transform.position = new Vector3(width / 2 - 2, height - 1, depth / 2);
            cubePositions(1);
            updateGhost();
            losing();
        }
        movement();
    }

    private void detection() // Recorre todo grid verificando donde están los cubos de la ficha activa y revisa: 
    {
        canmoveX0 = true;
        canmoveX1 = true;
        canmoveZ0 = true;
        canmoveZ1 = true;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] == 1)
                    {
                        if (y == 0 || grid[x, y - 1, z] == 2) // 1. Si algún cubo de la ficha tocó suelo u otra ficha ya estática
                        {
                            touching = true;
                        }
                        if (x == width - 1 || grid[x + 1, y, z] == 2) // 2. Si algún cubo está en el límite de X o Z. Esto para restringir el movimiento hacia ese lado y que no se salga la ficha
                        {
                            canmoveX1 = false;
                        }
                        if (x == 0 || grid[x - 1, y, z] == 2)
                        {
                            canmoveX0 = false;
                        }
                        if (z == depth - 1 || grid[x, y, z + 1] == 2)
                        {
                            canmoveZ1 = false;
                        }
                        if (z == 0 || grid[x, y, z - 1] == 2)
                        {
                            canmoveZ0 = false;
                        }
                    }
                    if (grid[x, y, z] == 3) // También revisa dónde están los cubos de ghost hasta que toquen suelo u otra ficha ya estática
                    {
                        if (y == 0 || grid[x, y - 1, z] == 2)
                        {
                            ground = true;
                        }
                    }
                }
            }

        }
    }

    private void updateGrid(int replacethis, int forthis) // Reemplaza cada vez que aparezca el primer argumento en grid por el segundo
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (grid[x, y, z] == replacethis)
                    {
                        grid[x, y, z] = forthis;
                    }
                }
            }
        }
    }

    private void cubePositions(int typeofcube) // Ya sea para actualTetro o ghostTetro, mira la posición de cada cubo que lo compone en ese instante y la escribe en grid (1 para actualTetro, 3 para ghostTetro)
    {
        Transform tetracube = actualTetro.transform;
        if (typeofcube == 3)
            tetracube = ghostTetro.transform;
        foreach (Transform cube in tetracube)
        {
            Vector3 gridPosition = cube.position;
            if (gridPosition.x >= 0 && gridPosition.x < width && gridPosition.y >= 0 && gridPosition.y < height && gridPosition.z >= 0 && gridPosition.z < depth)
            {
                grid[(int)gridPosition.x, (int)gridPosition.y, (int)gridPosition.z] = typeofcube;
            }
        }
    }

    private void convertIntoCubes() // Cuando un tetracubo cae, toma cubo por cubo y los reparte dependiendo de en que fila quedaron (estas se guardan en rows) esto para luego poder mirar si una fila se completó y eliminar sus cubos
    {
        for (int i = 3; i >= 0; i--)
        {
            Transform child = actualTetro.transform.GetChild(i);
            int rowNumber = (int)child.position.y;
            GameObject rowParent = GameObject.Find("row" + rowNumber);
            child.SetParent(rowParent.transform, true);
            cubesInRow[rowNumber]++;
        }
        Destroy(actualTetro);
    }

    private void eliminateRow() // Revisa fila por fila, si alguna está completa, la elimina y actualiza este cambio en grid y luego en el arreglo rows (bajando las filas arriba de esta)
    {
        for (int i = 0; i < height; i++)
        {
            if (cubesInRow[i] == width * depth)
            {
                leveling();
                for (int y = i + 1; y < height; y++)
                {
                    // Se bajan las filas superiores en la grilla
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            if (grid[x, y, z] == 2 || grid[x, y, z] == 0)
                            {
                                grid[x, y - 1, z] = grid[x, y, z];
                            }
                        }
                    }
                    // Se bajan las filas superiores en el GameObject rows y elimina la que se haya completado
                    GameObject rowParent = GameObject.Find("row" + (y - 1));
                    Transform[] childrenCopy = new Transform[rowParent.transform.childCount];
                    for (int j = 0; j < rowParent.transform.childCount; j++)
                    {
                        childrenCopy[j] = rowParent.transform.GetChild(j);
                    }
                    for (int k = 0; k < childrenCopy.Length; k++)
                    {
                        if (y - 1 == i)
                            Destroy(childrenCopy[k].gameObject);
                        else
                        {
                            Transform cube = childrenCopy[k];
                            cube.position = new Vector3(cube.position.x, cube.position.y - 1, cube.position.z);
                            cube.SetParent(GameObject.Find("row" + (y - 2)).transform);
                        }
                    }
                    cubesInRow[y - 1] = cubesInRow[y];
                }
                i--;
            }
        }
    }

    private void leveling() // Suma 100 puntos a score por cada fila hecha y por cada 2 filas se sube de nivel, haciendo que bajen más rápido las fichas
    {
        score += 100;
        scoreText.text = "Score: " + score.ToString();
        b = true;
        if (score % 200 == 0 && b == true)
        {
            level++;
            levelText.text = "Level: " + level.ToString();
            if (interval > 1.1f)
                interval--;
            else
                interval -= 0.1f;
            b = false;
        }
    }

    private void updateGhost() // Se elimina y luego crea ghostTetro instanciando actualTetro, luego va bajando fila por fila en donde está actualTetro, hasta que encuentra piso o algún cubo estático
    {
        Destroy(ghostTetro);
        ghostTetro = Instantiate(actualTetro, actualTetro.transform.position, Quaternion.identity);
        changeMaterial(Ghost_material);
        updateGrid(3, 0);
        cubePositions(3);
        for (int i = 0; i < height; i++)
        {
            detection();
            if (ground == false)
            {
                ghostTetro.transform.position = new Vector3(ghostTetro.transform.position.x, ghostTetro.transform.position.y - 1, ghostTetro.transform.position.z);
                updateGrid(3, 0);
                cubePositions(3);
            }
            if (ground == true)
            {
                updateGrid(3, 0);
                ground = false;
                cubePositions(1);
                break;
            }
        }
    }

    private void changeMaterial(Material newMaterial) // Cambia el material de ghostTetro para que no se vea igual a actualTetro
    {
        Transform tetracube = ghostTetro.transform;
        if (newMaterial != null)
        {
            foreach (Transform child in tetracube)
            {
                Renderer childRenderer = child.GetComponent<Renderer>();
                childRenderer.material = newMaterial;
            }
        }
    }

    private void losing() // Si apenas spawnea una ficha, ya se detecta un cubo abajo, se pausa el juego y aparece la pantalla de game over
    {
        detection();
        if (touching == true)
        {
            pause = true;
            Time.timeScale = 0;
            gameOverText.gameObject.SetActive(true);
            retry.gameObject.SetActive(true);
            musicSource.Pause();
        }
    }

    private void movement() // Revisa si la ficha activa se puede mover (traslación o rotación) sin que se sobrelape con algún cubo estático o se salga de los límites y luego realiza el movimiento, luego actualiza su posición
    {
        cubeConfirm();
        if (Input.GetKeyDown(KeyCode.Space)) // Mueve la ficha hasta abajo donde está su ghostTetro
        {
            actualTetro.transform.position = ghostTetro.transform.position;
            updateGrid(1, 0);
            cubePositions(1);
        }

        // Traslaciones
        else if (Input.GetKeyDown(KeyCode.LeftArrow) && canmoveX0)
        {
            actualTetro.transform.position = new Vector3(actualTetro.transform.position.x - 1, actualTetro.transform.position.y, actualTetro.transform.position.z);
            inGrid();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && canmoveX1)
        {
            actualTetro.transform.position = new Vector3(actualTetro.transform.position.x + 1, actualTetro.transform.position.y, actualTetro.transform.position.z);
            inGrid();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) && canmoveZ1)
        {
            actualTetro.transform.position = new Vector3(actualTetro.transform.position.x, actualTetro.transform.position.y, actualTetro.transform.position.z + 1);
            inGrid();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) && canmoveZ0)
        {
            actualTetro.transform.position = new Vector3(actualTetro.transform.position.x, actualTetro.transform.position.y, actualTetro.transform.position.z - 1);
            inGrid();
        }

        // Rotaciones
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) // Reversas
        {
            if (Input.GetKeyDown(KeyCode.A) && canRotate("X-"))
            {
                foreach (Transform child in actualTetro.transform)
                {
                    child.localPosition = new Vector3((int)child.localPosition.x, (int)child.localPosition.z, (int)-child.localPosition.y);
                }
                inGrid();
            }
            else if (Input.GetKeyDown(KeyCode.S) && canRotate("Y-"))
            {
                foreach (Transform child in actualTetro.transform)
                {
                    child.localPosition = new Vector3((int)child.localPosition.z, (int)child.localPosition.y, (int)-child.localPosition.x);
                }
                inGrid();
            }
            else if (Input.GetKeyDown(KeyCode.D) && canRotate("Z-"))
            {
                foreach (Transform child in actualTetro.transform)
                {
                    child.localPosition = new Vector3((int)-child.localPosition.y, (int)child.localPosition.x, (int)child.localPosition.z);
                }
                inGrid();
            }
        }
        else if (Input.GetKeyDown(KeyCode.A) && canRotate("X+")) // Normales
        {
            foreach (Transform child in actualTetro.transform)
            {
                child.localPosition = new Vector3((int)child.localPosition.x, (int)-child.localPosition.z, (int)child.localPosition.y);
            }
            inGrid();
        }
        else if (Input.GetKeyDown(KeyCode.S) && canRotate("Y+"))
        {
            foreach (Transform child in actualTetro.transform)
            {
                child.localPosition = new Vector3((int)-child.localPosition.z, (int)child.localPosition.y, (int)child.localPosition.x);
            }
            inGrid();
        }
        else if (Input.GetKeyDown(KeyCode.D) && canRotate("Z+"))
        {
            foreach (Transform child in actualTetro.transform)
            {
                child.localPosition = new Vector3((int)child.localPosition.y, (int)-child.localPosition.x, (int)child.localPosition.z);
            }
            inGrid();
        }
    }

    public void cubeConfirm() // Confirma que los cubos estén dentro del espacio de juego
    {
        if (tetroIndex == 6 && a == true)
        {
            foreach (Transform child in actualTetro.transform)
            {
                child.localPosition = new Vector3((int)child.localPosition.x, (int)-child.localPosition.z, (int)child.localPosition.y);
            }
            inGrid();
            a = false;
        }
        else if (tetroIndex != 6)
            a = true;
    }

    private void inGrid() // Limpia las posiciones pasadas de los cubos del tetracubo activo en grid, luego pone las posiciones actuales de los cubos y finalmente mueve ghostTetro a dónde debería caer ahora
    {
        updateGrid(1, 0);
        cubePositions(1);
        updateGhost();
    }

    private bool canRotate(string rotationAxis) // Simula la rotación que se quiera hacer para el tetracubo actual y verifica que ningún cubo vaya a estar donde ya se encuentra otro estático
    {
        List<Vector3> simulatedPositions = new List<Vector3>();
        Vector3 spatialpos = actualTetro.transform.position;
        foreach (Transform child in actualTetro.transform)
        {
            Vector3 rotatedPosition;
            switch (rotationAxis)
            {
                case "X+":
                    rotatedPosition = new Vector3(child.localPosition.x, -child.localPosition.z, child.localPosition.y);
                    break;
                case "X-":
                    rotatedPosition = new Vector3(child.localPosition.x, child.localPosition.z, -child.localPosition.y);
                    break;
                case "Y+":
                    rotatedPosition = new Vector3(-child.localPosition.z, child.localPosition.y, child.localPosition.x);
                    break;
                case "Y-":
                    rotatedPosition = new Vector3(child.localPosition.z, child.localPosition.y, -child.localPosition.x);
                    break;
                case "Z+":
                    rotatedPosition = new Vector3(child.localPosition.y, -child.localPosition.x, child.localPosition.z);
                    break;
                case "Z-":
                    rotatedPosition = new Vector3(-child.localPosition.y, child.localPosition.x, child.localPosition.z);
                    break;
                default:
                    return false;
            }

            Vector3 gridPosition = rotatedPosition + spatialpos;
            simulatedPositions.Add(gridPosition);

            // Verifica si la posición simulada está fuera de los límites de la grilla
            if (gridPosition.x < 0 || gridPosition.x >= width || gridPosition.y < 0 || gridPosition.z < 0 || gridPosition.z >= depth)
            {
                return false;
            }
        }

        // Verifica si alguna posición simulada colisionaría con un tetracubo estático
        foreach (Vector3 pos in simulatedPositions) //taka
        {
            if (grid[(int)pos.x, (int)pos.y, (int)pos.z] == 2)
            {
                return false;
            }
        }

        return true; // La rotación es viable
    }

    public void retryClick() // Si se hace click en el botón de ISERT COIN, se vuelve a cargar la escena (para reiniciar el juego)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}