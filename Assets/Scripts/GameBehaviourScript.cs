using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
// Este script se le aplica al GameObject Tetracube, en el que se van guardando una ficha hasta que cae (es como el objeto padre entonces las instrucciones aplican a los hijos)
// de ahí en adelante se dejan como GameObjects estáticos aparte (no les afectan más las instrucciones del script)
public class GameBehaviourScript : MonoBehaviour
{
    public GameObject[] tetracubes; // Arreglo de objetos en los que se meten las 7 diferentes fichas (tetracubos)
    public GameObject[] rows;

    private float interval = 3f;
    private float timer = 0.0f;
    private bool touching = false;
    private bool ground = false;
    private bool canmoveX1 = true;
    private bool canmoveX0 = true;
    private bool canmoveZ1 = true;
    private bool canmoveZ0 = true;
    private bool a = true;
    private bool b = true;
    private bool pause = false;

    private int width = 4;
    private int height = 11;
    private int depth = 4;
    private int[,,] grid;

    private int score = 0;
    private int level = 0;
    public TMP_Text scoreText;
    public TMP_Text levelText;
    public TMP_Text gameOverText;
    public Button retry;
    public Image nextTetro;
    public AudioSource musicSource;

    private int tetroIndex;
    private int tetroIndex1;
    private int[] cubesInRow;
    private GameObject actualTetro;
    private GameObject ghostTetro;
    private Vector3 spawnPosition;
    private Material Ghost_material;

    void Start() 
    {
        // Para hacer que vaya a targetFrameRate fps
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

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
        cubesInRow = new int[height];
        scoreText.text = "Score: " + score.ToString();
        levelText.text = "Level: 0";
        gameOverText.gameObject.SetActive(false);
        retry.gameObject.SetActive(false);
        musicSource = GetComponent<AudioSource>();
        
        Ghost_material = Resources.Load<Material>("Materials/Ghost_material");

        tetroIndex = UnityEngine.Random.Range(0, tetracubes.Length); // Elige aleatoriamente una de las fichas del arreglo
        tetroIndex1 = UnityEngine.Random.Range(0, tetracubes.Length);
        nextTetro.sprite = Resources.Load<Sprite>(tetroIndex1.ToString() + "_prefab");
        actualTetro = Instantiate(tetracubes[tetroIndex], transform.position, Quaternion.identity, transform); // Crea una copia de la ficha seleccionada y le asigna posición y rotación del GameObject Tetracube (y establece este como objeto padre)
        spawnPosition = new Vector3(width/2-2, height-1, depth/2); // Posición en la que empiezan los tetracubos
        actualTetro.transform.position = spawnPosition; // Se sincroniza la spawnPosition con la posición del GameObject Tetracube
        cubePositions(1);
        updateGhost();
    }

    void Update() 
    {
        if (pause == false)
        {
            detection();
            movement();
            timer += Time.deltaTime;
            if (timer >= interval && actualTetro.transform.position.y > 0 && touching == false)
            {
                actualTetro.transform.position = new Vector3(actualTetro.transform.position.x, actualTetro.transform.position.y - 1, actualTetro.transform.position.z);
                timer = 0.0f;
                updateGrid(1, 0);
                cubePositions(1);
            }
            if (actualTetro.transform.position.y == 0 || touching == true)
            {
                touching = false;
                updateGrid(1, 2);
                convertIntoCubes();
                eliminateRow();
                Destroy(actualTetro);
                tetroIndex = tetroIndex1;
                tetroIndex1 = UnityEngine.Random.Range(0, tetracubes.Length);
                nextTetro.sprite = Resources.Load<Sprite>(tetroIndex1.ToString() + "_prefab");
                actualTetro = Instantiate(tetracubes[tetroIndex], transform.position, Quaternion.identity, transform);
                actualTetro.transform.position = spawnPosition;
                cubePositions(1);
                updateGhost();
                losing();
            }
        }
    }

    private void leveling()
    {
        score++;
        scoreText.text = "Score: " + score.ToString();
        b = true;
        if (score % 2 == 0 && b == true)
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

    private void losing()
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

    public void retryClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void convertIntoCubes()
    {
        for (int i = 3; i >= 0; i--)
        {
            Transform child = actualTetro.transform.GetChild(i);
            int rowNumber = (int)child.position.y;
            GameObject rowParent = GameObject.Find("row" + rowNumber);
            child.SetParent(rowParent.transform, true);
            cubesInRow[rowNumber]++;
        }
    }

    private void eliminateRow() // si uno hace 2 de una, se elimina la de abajo y la siguiente se elimina pero cuando cae la pieza que sigue
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
                    // Se bajan las filas superiores en el GameObject rows
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

    private void cubePositions(int typeofcube)
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

    private void updateGrid(int replacethis, int forthis)
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

    private void changeMaterial(Material newMaterial)
    {
        Transform tetracube = actualTetro.transform;
        if (newMaterial == Ghost_material)
            tetracube = ghostTetro.transform;
        if (newMaterial != null)
        {
            foreach (Transform child in tetracube)
            {
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null)
                    childRenderer.material = newMaterial;
                else
                    print("No se encontró el material");
            }
        }
    }

    private void updateGhost()
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

    private void inGrid()
    {
        updateGrid(1, 0);
        cubePositions(1);
        updateGhost();
    }

    public void printGrid()
    {
        string actualgrid = "";
        for (int y = grid.GetLength(1) - 1; y >= 0; y--)
        {
            actualgrid = actualgrid + "y = " + y + "\n";
            for (int z = grid.GetLength(2)-1; z >= 0; z--)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    actualgrid = actualgrid + grid[x, y, z].ToString() + " ";
                }
                actualgrid = actualgrid + "     z = " + z + "\n";
            }
            actualgrid = actualgrid + "\n\n";
        }
        print(actualgrid);
        // Se cuenta comenzando desde 0
        // Cada cuadrado grande es un renglón de "y" (espacialmente, como si y se cortara a rebanadas, esto servirá luego para quitar dichos reenglones si todos están en 1)
        // En cada cuadrado, las columnas (Izquierda -> Derecha) es "x" mientras que las filas (Abajo -> Arriba) es "z" 
    }

    public void cubeConfirm()
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

    private void detection()
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
                        if (y == 0 || grid[x, y - 1, z] == 2)
                        {
                            touching = true;
                        }
                        if (x == width-1 || grid[x + 1, y, z] == 2)
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
                    if (grid[x, y, z] == 3)
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

    private bool canRotate(string rotationAxis)
    {
        // Simula la rotación del tetracubo actual
        List<Vector3> simulatedPositions = new List<Vector3>(); //tiki
        Vector3 spatialpos = actualTetro.transform.position; //tiki
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
                    return false; // Eje de rotación no válido
            }

            Vector3 gridPosition = rotatedPosition + spatialpos; //taka
            simulatedPositions.Add(gridPosition);

            // Verifica si la posición simulada está fuera de los límites de la grilla
            if (gridPosition.x < 0 || gridPosition.x >= width || gridPosition.y < 0 || gridPosition.z < 0 || gridPosition.z >= depth)
            {
                return false; // La rotación resultaría en una posición fuera de la grilla
            }
        }

        // Verifica si alguna posición simulada colisionaría con un tetracubo estático
        foreach (Vector3 pos in simulatedPositions) //taka
        {
            if (grid[(int)pos.x, (int)pos.y, (int)pos.z] == 2)
            {
                return false; // La rotación resultaría en una colisión
            }
        }

        return true; // La rotación es viable
    }

    private void movement()
    {
        cubeConfirm();
        if (Input.GetKeyDown(KeyCode.Space))
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
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
        else if (Input.GetKeyDown(KeyCode.A) && canRotate("X+"))
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
}