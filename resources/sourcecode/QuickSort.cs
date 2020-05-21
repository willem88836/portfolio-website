using System;

public class QuickSort
{
    private static void Sort(ref int[] array)
    {
        Sort(ref array, 0, array.Length - 1);
    }

    private static void Sort(ref int[] array, int low, int high)
    {
        // Storing initial low and high values for later use. 
        int ih = high;
        int il = low;

        if (high - low <= 1)
        {
            return;
        }

        int p = array[low];
        low++;

        while (low <= high)
        {
            // Select left element to swap.
            while (low <= high)
            {
                if (array[low] > p)
                {
                    break;
                }
                low++;
            }

            // Select right element to swap.
            while (low <= high)
            {
                if (array[high] < p)
                {
                    break;
                }
                high--;
            }

            // Swaps elements if necessary.
            if (low <= high)
            {
                int temp = array[low];
                array[low] = array[high];
                array[high] = temp;
            }
        }

        // Swapping the pivot with the last high value. 
        array[il] = array[high];
        array[high] = p;

        // Recursively sorts the left and right side of the array.
        Sort(ref array, low, ih);
        Sort(ref array, il, high - 1);
    }

    static void Main(string[] args)
    {
        Random random = new Random();
        int l = 64;
        int[] myArray = new int[l];

        // Fills the array with random values. 
        for (int i = 0; i < l; i++)
        {
            myArray[i] = random.Next(0, 500);
        }

        Sort(ref myArray);
    }
}
