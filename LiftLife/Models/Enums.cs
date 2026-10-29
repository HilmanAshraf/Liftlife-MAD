namespace LiftLife.Models;

/// <summary>Live availability of a community gym station.</summary>
public enum StationStatus
{
    Free = 0,
    Busy = 1,
    OutOfOrder = 2
}

/// <summary>Whether today's nutrition targets follow a training or rest day.</summary>
public enum DayType
{
    Training = 0,
    Rest = 1
}

/// <summary>Meal categories used to group logged food.</summary>
public enum MealCategory
{
    Breakfast = 0,
    Lunch = 1,
    Dinner = 2,
    Snacks = 3
}
