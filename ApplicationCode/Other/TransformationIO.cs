using System.IO;
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;


namespace Registration.ApplicationCode.Other;

/// <summary>
/// Class that exports and fetches expected transformation from and to a file
/// Extension - txt
/// </summary>
public class TransformationIO
{
	public static void ExportTransformation(string path, Transform3D transformation)
	{
        StreamWriter streamWriter = new StreamWriter(path + ".txt");

        var rotationMatrix = transformation.RotationMatrix;
        var translationVector = transformation.TranslationVector;

        streamWriter.WriteLine("R {0}x{1}", rotationMatrix.ColumnCount, rotationMatrix.RowCount);

        string elementContent = "";
        for(int i = 0; i < rotationMatrix.ColumnCount; i++)
        {
            for (int j = 0; j < rotationMatrix.RowCount; j++)
                elementContent += string.Format(" {0} ", rotationMatrix[j, i]);

        }
        streamWriter.WriteLine(elementContent);

        streamWriter.WriteLine("t {0}", translationVector.Count);

        elementContent = "";
        for (int i = 0; i < translationVector.Count; i++)
            elementContent += string.Format(" {0} ", translationVector[i]);

        streamWriter.WriteLine(elementContent);
        streamWriter.Close();
    }

    public static Transform3D FetchTransformation(string path)
    {
        StreamReader streamReader = new StreamReader(path);
        Matrix<double> matrix;
        Vector<double> vector;

        TransformationFetcher tf = new TransformationFetcher(streamReader);

        matrix = tf.FetchMatrix();
        vector = tf.FetchVector();

        if (matrix == null || vector == null)
            return null;

        return new Transform3D(matrix, vector);
    }

    class TransformationFetcher
    {
        private const int MATRIX_DIMENSION = 2;
        private const int VECTOR_DIMENSION = 1;

        private StreamReader reader;

        public TransformationFetcher(StreamReader reader)
        {
            this.reader = reader;
        }

        public Matrix<double> FetchMatrix()
        {
            Matrix<double> fetchedMatrix = FetchMatrixDimensions();
            if (fetchedMatrix == null)
                return null;

            if(!FetchMatrixValues(fetchedMatrix))
                return null;

            return fetchedMatrix;
        }

        public Vector<double> FetchVector()
        {
            Vector<double> fetchedVector = FetchVectorDimensions();
            if (fetchedVector == null)
                return null;

            if (!FetchVectorValues(fetchedVector))
                return null;

            return fetchedVector;
        }

        private bool FetchMatrixValues(Matrix<double> fetchedMatrix)
        {
            Regex regexPattern = new Regex("(-?\\d+([,\\.]\\d+)?([eE][-+]?\\d+)?)");
            string line;
            int numbersParsed = 0;


            MatchCollection matches;

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                matches = regexPattern.Matches(line);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {

                    if (!double.TryParse(match.Value.Replace(",", "."), out double parsedNumber))
                        continue;

                    fetchedMatrix[numbersParsed % fetchedMatrix.ColumnCount, numbersParsed / fetchedMatrix.RowCount] = parsedNumber;
                    numbersParsed++;

                    if (numbersParsed >= (fetchedMatrix.RowCount * fetchedMatrix.ColumnCount))
                        return true;
                }
            }

            return false;
        }

        private Matrix<double> FetchMatrixDimensions()
        {
            Regex regexPattern = new Regex("R\\s+(\\d+)x(\\d+)");
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                System.Text.RegularExpressions.Match match = regexPattern.Match(line);

                if (!match.Success)
                    continue;

                if (match.Groups.Count != (MATRIX_DIMENSION+1))
                    continue;

                int rowCount;
                int columnCount;

                if (!int.TryParse(match.Groups[1].Value, out columnCount))
                    continue;

                if (!int.TryParse(match.Groups[2].Value, out rowCount))
                    continue;

                return Matrix<double>.Build.Dense(rowCount, columnCount);
            }

            return null;
        }

        private Vector<double> FetchVectorDimensions()
        {
            Regex regexPattern = new Regex("t\\s+(\\d+)");
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                System.Text.RegularExpressions.Match match = regexPattern.Match(line);

                if (!match.Success)
                    continue;

                if (match.Groups.Count != (VECTOR_DIMENSION + 1))
                    continue;

                int vectorCount;

                if (!int.TryParse(match.Groups[1].Value, out vectorCount))
                    continue;

                return Vector<double>.Build.Dense(vectorCount);
            }

            return null;
        }

        private bool FetchVectorValues(Vector<double> fetchedVector)
        {
            Regex regexPattern = new Regex("(-?\\d+([,\\.]\\d+)?([eE][-+]?\\d+)?)");
            string line;
            int numbersParsed = 0;


            MatchCollection matches;

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                matches = regexPattern.Matches(line);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {

                    if (!double.TryParse(match.Value.Replace(",", "."), out double parsedNumber))
                        continue;

                    fetchedVector[numbersParsed] = parsedNumber;
                    numbersParsed++;

                    if (numbersParsed >= fetchedVector.Count)
                        return true;
                }
            }

            return false;
        }

        
    }
}

