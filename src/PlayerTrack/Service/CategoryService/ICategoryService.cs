using System.Collections.Generic;

namespace PlayerTrack
{
	public interface ICategoryService
	{
		void ClearDeletedCategories(List<TrackCategory> categories);
		TrackCategory GetCategory(int categoryId);
		int GetCategoryIndex(string categoryName);
		TrackCategory GetDefaultCategory();
		string[] GetCategoryNames();
		void MoveUpList(int id);
		void ProcessCategoryModifications();
		void SetPlayerPriority();
		void MoveDownList(int id);
		void AddCategory();
		void DeleteCategory(int index);
		void SaveCategories();
		void ResetCategories();
	}
}