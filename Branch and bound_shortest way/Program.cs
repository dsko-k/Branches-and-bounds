using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;

/*

Программа решает т.н. задачу коммивояжера: дан перечень городов, каждый из которых 
необходимо посетить один раз. Маршрут посещения городов должен быть кратчайшим, 
с последующим возвратом в исходный город.

Результат работы программы – кратчайший маршрут посещения городов и его протяженность. 
Решение отыскивается методом ветвей и границ. 

Матрица расстояний между городами задана в файле. 

Результаты записаны в файл Result.txt.

*/

namespace Branch_and_bound_shortest_way
{

    class Data
    {
        static string pathCurrent; // путь к папке, в которую записываются данные по умолчанию

        static Data()
        {
            pathCurrent = new DirectoryInfo(@".").FullName;
        }
        
                
        // Путь к родительской папке
        public string GetParentDirectory(string path)
        {
            string pattern = @"(\\)";

            Regex regex = new Regex(pattern);

            Match match = regex.Match(path);

            int index = 0;
            
            while (match.Success)
            {   
                index = match.Index;               

                match = match.NextMatch();
            }

            string parentDir = path.Remove(index);
            
                        
            return parentDir;
        }

        // Метод, получающий из текущего пути pathCurrent путь к папке проекта
        public string GetProjectDirectory()
        {
            string parent = GetParentDirectory(pathCurrent);

            parent = GetParentDirectory(parent);

            parent = GetParentDirectory(parent);
            
            return parent;
        }

        // Прочитать файл Matrix_Distances.txt с матрицей расстояний между городами 
        // файл Matrix_Distances.txt должен находиться в папке проекта
        public string[,] ReadFile()
        {
            string pathDir = GetProjectDirectory();

            string pathFile = Path.Combine(pathDir, "Matrix_Distances.txt");

            string[] result = null;

            string[,] trimRes = null;

            try
            {
                result = File.ReadAllLines(pathFile, Encoding.Default);

                // Пропустить названия городов (самую первую строку и самый первый столбец)

                trimRes = new string[result.Length - 1, result.Length - 1];

                for (int row = 1; row < result.Length; row++)
                {
                    string[] columns = result[row].Split('\t');

                    string[] trimColumns = new string[columns.Length - 1];

                    for (int col = 1; col < columns.Length; col++)
                    {
                        trimRes[row - 1, col - 1] = columns[col];
                    }

                }

                //Branches br = new Branches();

                //Console.WriteLine("\nМатрица расстояний, считанная из файла (без названий городов):\n");

                //br.ShowMatrix(trimRes);

                return trimRes;

            }
            catch (FileNotFoundException)
            {
                Message("Не найден файл c матрицей расстояния по следующему пути:\n\n " + pathFile, ConsoleColor.Red, true);

                
                if (!new DirectoryInfo(pathDir).Exists)
                {
                    Message("\nДиректории, содержащей файл " + pathDir + ", НЕ существует!", ConsoleColor.Red, true);                                        
                }
                
                return trimRes;
            }

            
        }

        // Записать результат в файл: города кратчайшего маршрута и его протяженность. Файл располагается в папке проекта
        public void WriteFile(string text)
        {
            MemoryStream memory = new MemoryStream();
            BufferedStream buffer = new BufferedStream(memory);
            StreamWriter writer = new StreamWriter(buffer);

            writer.Write(text);
            writer.Flush();

            string pathFile = Path.Combine(GetProjectDirectory(), "Result.txt");

            FileStream file = File.Open(pathFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            memory.WriteTo(file);

            FileInfo fileInfo = new FileInfo(pathFile);

            memory.Close();
            

            Message("\nРезультат поиска кратчайшего маршрута записан в файл ", ConsoleColor.Green, false);

            Message(fileInfo.Name, ConsoleColor.Yellow, true);

            Message("\nПуть к нему:\n", ConsoleColor.Gray, true);            

            Message(fileInfo.FullName, ConsoleColor.Green, true);


            file.Close();
            writer.Close();
            buffer.Close();
        }


        // Вывести текст в консоль заданным цветом 
        public void Message(string text, ConsoleColor color, bool isLine)
        {
            Console.ForegroundColor = color;
            
            if (isLine) // записывать в строку
            {
                Console.WriteLine(text);
            }
            else // дописывать текст к строке
            {
                Console.Write(text);
            }            

            Console.ForegroundColor = ConsoleColor.Gray;
        }

    }


    class Branches
    {
        Data data;

        public Branches()
        {
            data = new Data();
        }
        
        // Получение массива, каждый элемент которого - минимальный элемент по строке (true) или по столбцу (false)
        double[] MinElement(string[,] distances, bool isRow)
        {
            ShowMatrix(distances, "Матрица расстояний");

            // double[,] distances - квадратная матрица, количсество строк и столбцов равно sqrt(distances.Length)
            // 0-я строка - условное обозначение города отправления (начиная с 0)
            // 0-й столбец - условное обозначение города прибытия (начиная с 0) 

            int length = (int)Math.Sqrt(distances.Length);

            double[] result = new double[length];


            if (isRow)
            {
                Console.WriteLine("\nМинимальный элемент каждой строки:\n");

                double minElem = 0;

                for (int row = 1; row < length; row++)
                {
                    minElem = MinRow(distances, row);

                    result[row] = minElem;

                    Console.WriteLine(minElem + "\n");
                }

            }
            else
            {
                Console.WriteLine("\nМинимальный элемент каждого столбца:\n");

                double minElem = 0;

                Console.Write("\t");

                for (int col = 1; col < length; col++)
                {
                    minElem = MinColumn(distances, col);

                    result[col] = minElem;

                    Console.Write(minElem + "\t");
                }

            }

            Console.WriteLine("\n");

            return result;
        }
        


        // Редукция строк - вычитание из каждого элемента в строке соответствующее значение найденного минимума этой строки
        // либо 
        // Редукция столбцов - вычитание из каждого элемента матрицы соответствующее ему значение найденного минимума этого столбца 
        string[,] Reduction(string[,] distances, bool isRow)
        {
            double[] minElements = MinElement(distances, isRow);

            int length = (int)Math.Sqrt(distances.Length);

            string[,] reduction = new string[length, length];

            if (isRow)
            {                

                for (int row = 0; row < length; row++)
                {
                    for (int col = 0; col < length; col++)
                    {
                        if (row != 0 & col != 0)
                        {
                            reduction[row, col] = (double.Parse(distances[row, col]) - minElements[row]).ToString();
                        }
                        else
                        {
                            reduction[row, col] = distances[row, col];
                        }


                    }
                }

               
                ShowMatrix(reduction, "После редукции строк (вычитание из элементов строки минимума этой строки)");
            }
            else
            {
               
                for (int row = 0; row < length; row++)
                {
                    for (int col = 0; col < length; col++)
                    {
                        if (row != 0 & col != 0)
                        {
                            reduction[row, col] = (double.Parse(distances[row, col]) - minElements[col]).ToString();
                        }
                        else
                        {
                            reduction[row, col] = distances[row, col];
                        }
                    }
                }

                
                ShowMatrix(reduction, "После редукции столбцов (вычитание из элементов столбца минимума этой строки)");
            }


            return reduction;
        }


        // Минимальный элемент в строке
        double MinRow(string[,] distances, int row)
        {
            int length = (int)Math.Sqrt(distances.Length);

            double minElem = double.Parse(distances[row, 1]);

            for (int col = 2; col < length; col++)
            {
                if (double.Parse(distances[row, col]) < minElem)
                {
                    minElem = double.Parse(distances[row, col]);
                }
            }

            return minElem;
        }

        // Минимальный элемент в строке, без учёта элемента по индексу int skipColIndex
        double MinRow(string[,] distances, int row, int skipColIndex)
        {
            int length = (int)Math.Sqrt(distances.Length);

            double minElem = 0;

            if (length > 2 && skipColIndex == 1)
            {
                minElem = double.Parse(distances[row, 2]);

                for (int col = 3; col < length; col++)
                {
                    if (double.Parse(distances[row, col]) < minElem)
                    {
                        minElem = double.Parse(distances[row, col]);
                    }
                }
            }
            else if (length > 2)
            {
                minElem = double.Parse(distances[row, 1]);

                for (int col = 2; col < length; col++)
                {
                    if (col != skipColIndex && double.Parse(distances[row, col]) < minElem)
                    {
                        minElem = double.Parse(distances[row, col]);
                    }
                }
            }



            return minElem;
        }


        // Минимальный элемент в столбце
        double MinColumn(string[,] distances, int column)
        {
            int length = (int)Math.Sqrt(distances.Length);

            double minElem = double.Parse(distances[1, column]);

            for (int row = 2; row < length; row++)
            {
                if (double.Parse(distances[row, column]) < minElem)
                {
                    minElem = double.Parse(distances[row, column]);
                }
            }

            return minElem;
        }


        // Минимальный элемент в столбце, без учёта элемента по индексу int skipRowIndex
        double MinColumn(string[,] distances, int column, int skipRowIndex)
        {
            int length = (int)Math.Sqrt(distances.Length);

            double minElem = 0;


            if (length > 2 && skipRowIndex == 1)
            {
                minElem = double.Parse(distances[2, column]);

                for (int row = 3; row < length; row++)
                {
                    if (double.Parse(distances[row, column]) < minElem)
                    {
                        minElem = double.Parse(distances[row, column]);
                    }
                }
            }
            else if (length > 2)
            {
                minElem = double.Parse(distances[1, column]);

                for (int row = 2; row < length; row++)
                {
                    if (skipRowIndex != row && double.Parse(distances[row, column]) < minElem)
                    {
                        minElem = double.Parse(distances[row, column]);
                    }
                }
            }



            return minElem;
        }


        //п.6 Матрица с оценками в нулевых клетках
        /*
        Для каждой нулевой клетки получившейся преобразованной матрицы находим «оценку»: 
        это сумма минимального элемента по строке и минимального элемента по столбцу, в которых размещена данная нулевая 
        клетка. Найденные ранее минимальные значения по строке и по столбцу не учитываются. 
        Полученную оценку записываем рядом с нулем, в скобках.
        */
        string[,] Evaluation(string[,] distances)
        {
            Console.WriteLine("\nВычисление оценок нулевых клеток:\n");

            int length = (int)Math.Sqrt(distances.Length);

            string[,] result = new string[length, length];


            for (int row = 0; row < length; row++)
            {
                for (int col = 0; col < length; col++)
                {
                    if (row != 0 && col != 0 && double.Parse(distances[row, col]) == 0)
                    {
                        double sumMin = MinRow(distances, row, col) + MinColumn(distances, col, row);

                        result[row, col] = "0 (" + sumMin.ToString() + ")";
                    }
                    else
                    {
                        result[row, col] = distances[row, col].ToString();
                    }
                }
            }

            ShowMatrix(result, "Матрица с оценками в нулевых клетках. Оценки - в скобках");
            

            ///////////////////////// удалить (для проверки)
            int indexRowMax = 0;
            int indexColMax = 0;

            FindMaxEvaluation(result, out indexRowMax, out indexColMax);

            return result;
        }


        // Найти в матрице с оценками в нулевых клетках - максимальное значение, которое в скобках
        bool FindMaxEvaluation(string[,] evaluation, out int indexRowMax, out int indexColMax)
        {
            int rowMax = -1; // индекс строки, в которой находится элемент с максимальной оценкой
            int colMax = -1; // индекс солбца, в котором находится элемент с максимальной оценкой

            bool isFound = false;

            int length = (int)Math.Sqrt(evaluation.Length);

            string pattern = @"0 \((\d+)\)";

            Regex regex = new Regex(pattern);

            double currentEval = 0;
            int counterZero = 0;

            for (int row = 1; row < length; row++)
            {
                for (int col = 1; col < length; col++)
                {

                    if (regex.IsMatch(evaluation[row, col]))
                    {
                        Match match = regex.Match(evaluation[row, col]);


                        if (counterZero == 0)
                        {
                            currentEval = double.Parse(match.Groups[1].Value);

                            rowMax = row;
                            colMax = col;

                            isFound = true;
                        }
                        else
                        {
                            if (double.Parse(match.Groups[1].Value) > currentEval)
                            {
                                currentEval = double.Parse(match.Groups[1].Value);

                                rowMax = row;
                                colMax = col;

                                isFound = true;
                            }
                        }


                        counterZero++;
                    }
                }
            }


            indexRowMax = rowMax;
            indexColMax = colMax;

            if (isFound)
            {
                Console.WriteLine("Максимальная оценка: {0} . Строка = {1}, столбец = {2}", evaluation[indexRowMax, indexColMax], indexRowMax, indexColMax);
            }
            else
            {
                Console.WriteLine("Не найдена хотя бы одна нулевая оценка");
            }


            return isFound;
        }

        // Найти индекс строки в 0-м столбце, значение по которому равно value
        int FindValueRow(string[,] distances, string value)
        {
            int resultIndex = -1;

            int length = (int)Math.Sqrt(distances.Length);

            for (int row = 1; row < length; row++)
            {
                if (value == distances[row, 0])
                {
                    resultIndex = row;
                    break;
                }
            }

            return resultIndex;
        }

        // Найти индекс столбца в 0-й строке, значение по которому равно value
        int FindValueCol(string[,] distances, string value)
        {
            int resultIndex = -1;

            int length = (int)Math.Sqrt(distances.Length);

            for (int col = 1; col < length; col++)
            {
                if (value == distances[0, col])
                {
                    resultIndex = col;
                    break;
                }
            }

            return resultIndex;
        }



        // Удалить в distances строку, соответствующую городу, указанному в строке с индексом 0 и 
        // удалить в distances столбец, соответствующую городу, указанному в столбце с индексом 0
        string[,] Delete(string[,] distances, int cityRow, int cityCol)
        {
            int length = (int)Math.Sqrt(distances.Length);

            int findValueRow = FindValueRow(distances, cityRow.ToString());
            int findValueCol = FindValueCol(distances, cityCol.ToString());

            string[,] newDistances = null;

            if (findValueRow == -1 & findValueCol == -1)
            {
                newDistances = new string[length, length];
            }
            else
            {
                newDistances = new string[length - 1, length - 1];
            }


            Console.WriteLine("\nУдаление в матрице расстояний СТРОКИ с записями города {0} и удаление СТОЛБЦА с записями города {1} (если ранее не удалены):\n", cityRow, cityCol);

            int rowNewDistances = 0;

            for (int row = 0; row < length; row++)
            {
                int colNewDistances = 0;



                if (row == findValueRow)
                {
                    continue;
                }


                for (int col = 0; col < length; col++)
                {
                    if (col == findValueCol)
                    {
                        continue;
                    }


                    newDistances[rowNewDistances, colNewDistances] = distances[row, col];

                    colNewDistances++;

                }

                rowNewDistances++;
            }


            ShowMatrix(newDistances, "Матрица расстояний после удаления указанных строк и столбцов");

            return newDistances;
        }



        List<int[]> ShortestWays(string[,] distances, int counter, int maxCounter, List<int[]> previous) // double[,] distances поместить в тело
        {
            data.Message("\n\t\t\tИтерация № " + (counter + 1).ToString() +":\n", ConsoleColor.Yellow, true);
            
            List<int[]> shortestWays = new List<int[]>();

            foreach (var item in previous)
            {
                shortestWays.Add(item);
            }

            
            // 1. Редукция строк 
            string[,] reductRows = Reduction(distances, true);

            // 2. Редукция столбцов        
            string[,] reductColumnsRows = Reduction(reductRows, false);

            // 3. Вычисление оценок нулевых клеток
            string[,] evaluation = Evaluation(reductColumnsRows);

            int indexRowMax = 0, indexColMax = 0;


            // рекурсия: удаление из матрицы расстояний уже пройденных городов и 
            // повторение шагов алгоритма с оставшимися строками и столбцами матрицы расстояний

            if (counter < maxCounter - 2 && FindMaxEvaluation(evaluation, out indexRowMax, out indexColMax)) // базовое ограничение
            {
                int foundRow = (int)double.Parse(evaluation[indexRowMax, 0]);
                int foundCol = (int)double.Parse(evaluation[0, indexColMax]);

                shortestWays.Add(new[] { foundRow, foundCol });

                data.Message("Часть кратчайшего пути: из Города № "+ evaluation[indexRowMax, 0] + " в Город № " + evaluation[0, indexColMax], ConsoleColor.Red, true);
                

                foreach (var item in shortestWays)
                {
                    distances = Delete(distances, item.ToArray()[0], item.ToArray()[1]);
                }

                counter++;

                // рекурсия
                return ShortestWays(distances, counter, maxCounter, shortestWays);
                                
            }
            else
            {
                return shortestWays;
            }
            
        }


        // Удалить дубликаты путей 
        int[] NonDuplicates(List<int[]> waysDuplicates)
        {
            List<int> nonDuplicates = new List<int>();

            nonDuplicates.Add(waysDuplicates[0][0]);
            nonDuplicates.Add(waysDuplicates[0][1]);

            

            for (int i = 1; i < waysDuplicates.Count; i++)
            {
                
                for (int k = 0; k <= 1; k++) // обход массива элемента List<int[]> waysDuplicates
                {
                    bool isDuplicate = false;

                    foreach (int item in nonDuplicates)
                    {
                        if (item == waysDuplicates[i][k])
                        {
                            isDuplicate = true;
                            break;
                        }

                    }


                    if (isDuplicate == false & i != waysDuplicates.Count - 1)
                    {
                        nonDuplicates.Add(waysDuplicates[i][k]);
                    }
                    else if (i == waysDuplicates.Count - 1)
                    {
                        if (isDuplicate && waysDuplicates[i][k] == nonDuplicates[0])
                        {
                            nonDuplicates.Add(waysDuplicates[i][k]);
                        }
                        else if (! isDuplicate)
                        {
                            nonDuplicates.Add(waysDuplicates[i][k]);
                        }
                    }

                }
            }
            

            return nonDuplicates.ToArray();
        }


        // Результат
        public int[] Result()
        {
            string[,] distances = data.ReadFile(); 


            int maxCounter = (int)Math.Sqrt(distances.Length);

            List<int[]> shortestWays = ShortestWays(distances, 0, maxCounter, new List<int[]> { });

            int[] result = NonDuplicates(shortestWays);
            

            WholeDistance(distances, result);


            return result;
        }


        // Протяженность кратчайшего маршрута
        double WholeDistance(string[,] distances, int[] shortestWay)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(String.Format("{0}\t\tНайденный маршрут кратчайшего пути: {0}", Environment.NewLine));


            double sum = 0;

            int indexRow = 0;
            int indexCol = 0;

            data.Message("\n\tКратчайший путь (нумерация городов с 0):\n" , ConsoleColor.Yellow, true);
            
                        
            for (int i = 0; i < shortestWay.Length-1; i++)
            {                
                indexRow = shortestWay[i];
                indexCol = shortestWay[i+1];                

                Console.WriteLine("Из города № {0} в город № {1} (расстояние {2} км.)", shortestWay[i], shortestWay[i+1], distances[indexRow + 1, indexCol + 1]);

                builder.AppendLine(String.Format("Из города № {0} в город № {1} (расстояние {2} км.)", shortestWay[i], shortestWay[i + 1], distances[indexRow + 1, indexCol + 1]));
                
                sum = sum + double.Parse(distances[indexRow + 1, indexCol + 1]);
            }
            
            Console.WriteLine("\nПротяженность кратчайшего маршрута: {0} км.", sum);

            


            builder.AppendLine(String.Format("{0}Протяженность кратчайшего маршрута: {1} км.{0}{0}", Environment.NewLine, sum));


            // Записать в файл с результатом данные из матрицы расстояний

            string pathMatrixDist = Path.Combine(data.GetProjectDirectory(), "Matrix_Distances.txt");

            string[] matrixDist = File.ReadAllLines(pathMatrixDist, Encoding.Default);

            builder.AppendLine(String.Format("{0}\t\tМатрица расстояний:{0}", Environment.NewLine, sum));


            foreach (var item in matrixDist)
            {
                builder.AppendLine(item);
            }
            
            
            data.WriteFile(builder.ToString());


            return sum;
        }


        // Показать матрицу расстояний
        void ShowMatrix<T>(T[,] distances, string message)
        {
            data.Message(message + ":\n" , ConsoleColor.White, true);
                        
            // string[,] distances - квадратная матрица, с количеством строк и столбцов, равным sqrt(distances.Length)

            int length = (int)Math.Sqrt(distances.Length);

            for (int row = 0; row < length; row++)
            {

                for (int col = 0; col < length; col++)
                {
                    
                    if (row == 0 | col == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else if (row != 0 | col != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    

                    Console.Write("{2}\t", row, col, distances[row, col]);

                    Console.ForegroundColor = ConsoleColor.Gray;
                }

                Console.WriteLine("\n");

            }

            data.Message("\nБирюзовый ", ConsoleColor.Cyan, false);
            data.Message("- номер города", ConsoleColor.Gray, true);

            data.Message("Зелёный ", ConsoleColor.Green, false);
            data.Message("- расстояние между городами\n", ConsoleColor.Gray, true);

            Console.WriteLine(new string('-', 30));
        }





    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.BufferHeight = 1000;
            Console.SetWindowSize(110, 40); 


            Branches br = new Branches();

            br.Result();

            

            Console.ReadKey();
        }
    }
}
