// ReSharper disable EventUnsubscriptionViaAnonymousDelegate

namespace PlayerTrack
{
	public class SettingsPresenter : PresenterBase
	{
		private readonly SettingsView _settingsView;

		public SettingsPresenter(IPlayerTrackPlugin plugin) : base(plugin)
		{
			_view = new SettingsView();
			_settingsView = (SettingsView) _view;
			InitSettings();
			AddListeners();
			_view.ShowView();
		}

		public void InitSettings()
		{
			_settingsView.NextCategories = _plugin.CategoryService.GetCategoriesCopy();
			_settingsView.Configuration = _plugin.Configuration;
			_settingsView.ContentNames = _plugin.GetContentNames();
			_settingsView.ContentIds = _plugin.GetContentIds();
			_settingsView.Icons = FontAwesomeUtil.Icons;
			_settingsView.IconNames = FontAwesomeUtil.IconNames;
		}

		public void AddListeners()
		{
			_settingsView.ConfigUpdated += SettingsViewOnConfigUpdated;
			_settingsView.RequestCategoryDelete += SettingsViewOnRequestCategoryDelete;
			_settingsView.RequestCategoryReset += SettingsViewOnRequestCategoryReset;
			_settingsView.LanguageUpdated += SettingsViewOnLanguageUpdated;
			_settingsView.RequestCategoryAdd += SettingsViewOnRequestCategoryAdd;
			_settingsView.RequestCategoryUpdate += SettingsViewOnRequestCategoryUpdate;
			_settingsView.RequestCategoryMoveUp += SettingsViewOnRequestCategoryMoveUp;
			_settingsView.RequestCategoryMoveDown += SettingsViewOnRequestCategoryMoveDown;
			_settingsView.RequestResetIcons += SettingsViewOnRequestResetIcons;
			_settingsView.RequestPrintHelp += SettingsViewOnRequestPrintHelp;
			_plugin.CategoryService.CategoriesUpdated += OnCategoriesUpdated;
		}

		private void SettingsViewOnRequestPrintHelp(object sender, bool e)
		{
			_plugin.PrintHelpMessage();
		}

		private void SettingsViewOnRequestResetIcons(object sender, bool e)
		{
			_plugin.SetDefaultIcons();
			_plugin.SaveConfig();
		}

		private void SettingsViewOnRequestCategoryMoveDown(object sender, int e)
		{
			_plugin.CategoryService.MoveDownList(e);
		}

		private void SettingsViewOnRequestCategoryMoveUp(object sender, int e)
		{
			_plugin.CategoryService.MoveUpList(e);
		}

		private void SettingsViewOnRequestCategoryUpdate(object sender, TrackCategory e)
		{
			_plugin.CategoryService.UpdateCategory(e);
		}

		private void SettingsViewOnRequestCategoryAdd(object sender, bool e)
		{
			_plugin.CategoryService.AddCategory();
		}

		private void SettingsViewOnLanguageUpdated(object sender, int e)
		{
			_plugin.Localization.SetLanguage(e);
		}

		private void SettingsViewOnRequestCategoryReset(object sender, bool e)
		{
			_plugin.CategoryService.ResetCategories();
		}

		public void Dispose()
		{
			_settingsView.ConfigUpdated -= SettingsViewOnConfigUpdated;
			_settingsView.RequestCategoryDelete -= SettingsViewOnRequestCategoryDelete;
			_settingsView.RequestCategoryReset -= SettingsViewOnRequestCategoryReset;
			_settingsView.LanguageUpdated -= SettingsViewOnLanguageUpdated;
			_settingsView.RequestCategoryAdd -= SettingsViewOnRequestCategoryAdd;
			_settingsView.RequestCategoryUpdate -= SettingsViewOnRequestCategoryUpdate;
			_settingsView.RequestCategoryMoveUp -= SettingsViewOnRequestCategoryMoveUp;
			_settingsView.RequestCategoryMoveDown -= SettingsViewOnRequestCategoryMoveDown;
			_settingsView.RequestResetIcons -= SettingsViewOnRequestResetIcons;
			_settingsView.RequestPrintHelp -= SettingsViewOnRequestPrintHelp;
			_plugin.CategoryService.CategoriesUpdated -= OnCategoriesUpdated;
		}

		private void OnCategoriesUpdated(object sender, bool e)
		{
			_settingsView.NextCategories = _plugin.CategoryService.GetCategoriesCopy();
			_settingsView.IsCategoryDataUpdated = true;
		}

		private void SettingsViewOnRequestCategoryDelete(object sender, int e)
		{
			_plugin.CategoryService.DeleteCategory(e);
		}

		private void SettingsViewOnConfigUpdated(object sender, bool e)
		{
			_plugin.SaveConfig();
		}
	}
}