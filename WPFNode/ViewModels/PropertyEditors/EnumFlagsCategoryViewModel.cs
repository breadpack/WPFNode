using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using WPFNode.Interfaces;

namespace WPFNode.ViewModels.PropertyEditors
{
    /// <summary>
    /// 열거형 플래그를 카테고리로 그룹화하는 뷰모델
    /// </summary>
    public class EnumFlagsCategoryViewModel : INotifyPropertyChanged
    {
        private readonly EnumFlagsPropertyViewModel _parent;
        private bool _isExpanded;
        private string _searchText = string.Empty;
        private readonly List<EnumValueViewModel> _allValues = new();
        private ObservableCollection<EnumValueViewModel> _filteredValues = new();
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumFlagsCategoryViewModel(string name, IEnumerable<EnumValueViewModel> values, EnumFlagsPropertyViewModel parent)
        {
            Name = name;
            _parent = parent;
            _allValues.AddRange(values);
            _filteredValues = new ObservableCollection<EnumValueViewModel>(_allValues);
            
            // 기본적으로 첫 번째 카테고리만 펼침
            if (Name == "일반" || Name == "기본" || Name == "Common" || _allValues.Count <= 5)
            {
                IsExpanded = true;
            }
        }

        /// <summary>
        /// 카테고리 이름
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// 카테고리의 열거형 값 목록
        /// </summary>
        public ObservableCollection<EnumValueViewModel> Values => _filteredValues;
        
        /// <summary>
        /// 카테고리의 모든 열거형 값 목록
        /// </summary>
        public IReadOnlyList<EnumValueViewModel> AllValues => _allValues;
        
        /// <summary>
        /// 이 카테고리가 펼쳐져 있는지 여부
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        
        /// <summary>
        /// 검색 필터 텍스트
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    FilterValues();
                    OnPropertyChanged(nameof(SearchText));
                }
            }
        }
        
        /// <summary>
        /// 이 카테고리의 모든 값이 선택되었는지 여부
        /// </summary>
        public bool AreAllSelected => Values.Count > 0 && Values.All(v => v.IsSelected);
        
        /// <summary>
        /// 이 카테고리의 일부 값이 선택되었는지 여부
        /// </summary>
        public bool AreSomeSelected => Values.Any(v => v.IsSelected) && !AreAllSelected;
        
        /// <summary>
        /// 표시되는 항목 수
        /// </summary>
        public int VisibleCount => Values.Count;
        
        /// <summary>
        /// 총 항목 수
        /// </summary>
        public int TotalCount => _allValues.Count;
        
        /// <summary>
        /// 이 카테고리가 표시되어야 하는지 여부
        /// </summary>
        public bool IsVisible => Values.Count > 0;

        /// <summary>
        /// 카테고리의 모든 값을 설정하거나 해제
        /// </summary>
        public void SetAllValues(bool isSelected)
        {
            foreach (var value in _allValues)
            {
                value.IsSelected = isSelected;
            }
            
            OnPropertyChanged(nameof(AreAllSelected));
            OnPropertyChanged(nameof(AreSomeSelected));
        }
        
        /// <summary>
        /// 검색어에 따라 값 필터링
        /// </summary>
        public void FilterValues()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                // 검색어가 없으면 모든 값 표시
                _filteredValues.Clear();
                foreach (var value in _allValues)
                {
                    _filteredValues.Add(value);
                }
            }
            else
            {
                // 검색어가 있으면 필터링
                _filteredValues.Clear();
                var filtered = _allValues.Where(v => 
                    v.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
                
                foreach (var value in filtered)
                {
                    _filteredValues.Add(value);
                }
                
                // 검색 결과가 있으면 자동으로 카테고리 펼침
                if (_filteredValues.Count > 0)
                {
                    IsExpanded = true;
                }
            }
            
            OnPropertyChanged(nameof(Values));
            OnPropertyChanged(nameof(VisibleCount));
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(AreAllSelected));
            OnPropertyChanged(nameof(AreSomeSelected));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
