// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Graph;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.UI.Controls.Graph
{
    /// <summary>
    /// Defines the events for the <see cref="PeoplePicker"/> control.
    /// </summary>
    public partial class PeoplePicker : Control
    {
        private static void AllowMultiplePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as PeoplePicker;
            if (!control.AllowMultiple)
            {
                control.Selections.Clear();
                control.RaiseSelectionChanged();
                control._searchBox.Text = string.Empty;
            }
        }

        private void ClearAndHideSearchResultListBox()
        {
            SearchResultList.Clear();
            _searchResultListBox.Visibility = Visibility.Collapsed;
        }

        private async void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textboxSender = (TextBox)sender;
            string searchText = textboxSender.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearAndHideSearchResultListBox();
                return;
            }

            _loading.IsActive = true;

            searchText = Regex.Replace(searchText, "[^0-9a-zA-Z .@]", string.Empty);
            int cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = searchText;
            textboxSender.SelectionStart = cursorPosition;

            try
            {
                Task<GraphServiceClient> graphClient = AadAuthenticationManager.Instance.GetGraphServiceClientAsync();

                var options = new List<QueryOption>
                {
                    new QueryOption("$search", searchText)
                };
                IUserPeopleCollectionPage peopleList = await (await graphClient).Me.People.Request(options).GetAsync();

                if (peopleList.Any())
                {
                    List<Person> searchResult = peopleList.Where(
                        u => !string.IsNullOrWhiteSpace(u.UserPrincipalName)).ToList();

                    // Remove all selected items
                    foreach (Person selectedItem in Selections)
                    {
                        searchResult.RemoveAll(u => u.UserPrincipalName == selectedItem.UserPrincipalName);
                    }

                    SearchResultList.Clear();
                    var result = SearchResultLimit > 0
                        ? searchResult.Take(SearchResultLimit).ToList()
                        : searchResult;
                    foreach (var item in result)
                    {
                        SearchResultList.Add(item);
                    }

                    _searchResultListBox.Visibility = Visibility.Visible;
                }
                else
                {
                    ClearAndHideSearchResultListBox();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _loading.IsActive = false;
            }
        }

        private void SearchResultListBox_OnSelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (!((sender as ListBox)?.SelectedItem is Person person))
            {
                return;
            }

            if (!AllowMultiple && Selections.Any())
            {
                Selections.Clear();
                Selections.Add(person);
            }
            else
            {
                Selections.Add(person);
            }
            RaiseSelectionChanged();

            _searchBox.Text = string.Empty;
        }

        private void DeleteSelectedItem(object parameter)
        {
            var userId = parameter as string;
            Person target = Selections.FirstOrDefault(u => u.Id == userId);
            if (target != null)
            {
                Selections.Remove(target);
                RaiseSelectionChanged();
            }
        }

        private void RaiseSelectionChanged()
        {
            this.SelectionChanged?.Invoke(this, new PeopleSelectionChangedEventArgs(this.Selections));
        }
    }
}