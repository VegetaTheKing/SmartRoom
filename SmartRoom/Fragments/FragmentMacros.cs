﻿using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using SmartRoom.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartRoom.Fragments
{
    public class FragmentMacros : Fragment
    {
        private readonly MacrosManager _macrosManager;
        private Fragments.FragmentPopupValue _popup;

        public FragmentMacros(Managers.MacrosManager macrosManager)
        {
            _macrosManager = macrosManager;
            _macrosManager.Macros.Add(new Models.MacroModel()
            { 
                Enabled = true,
                Name = "Test",
                Repeat = true,
                Running = false,
                Items = new System.Collections.ObjectModel.ObservableCollection<Interfaces.IMacroItemModel>()
                {
                    new Models.SliderSwitchModel() { Title="First", Enabled=true, Fade=true, Pin="10", Value=1F },
                    new Models.DelayMacroItemModel() { Enabled=true, Delay=1000 },
                    new Models.ToggleSwitchModel() { Title="Arduino", Enabled=true, Fade=true, Pin="D2", Toggle=true },
                    new Models.SliderSwitchModel() { Title="First", Enabled=true, Fade=true, Pin="10", Value=0F },
                    new Models.DelayMacroItemModel() { Enabled=true, Delay=1000 },
                    new Models.ColorSwitchModel() { Title="Color test nice", Enabled = true,  Fade=true, RedPin="1", GreenPin="2", BluePin="3", Color=new Models.ColorModel(255,128,192) },
                    new Models.DelayMacroItemModel() { Enabled=true, Delay=1000 },
                    new Models.ColorSwitchModel() { Title="Color test nice", Enabled = true,  Fade=true, RedPin="1", GreenPin="2", BluePin="3", Color=new Models.ColorModel(0,0,0) },
                    new Models.ToggleSwitchModel() { Title="Arduino", Enabled=true, Fade=false, Pin="D2", Toggle=false },
                    new Models.DelayMacroItemModel() { Enabled=true, Delay=1000 }
                }
            });
            _macrosManager.StartMacro(_macrosManager.Macros[0]);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var v = inflater.Inflate(Resource.Layout.content_macros, container, false);

            var list = v.FindViewById<ListView>(Resource.Id.macros_list);
            var text = v.FindViewById<TextView>(Resource.Id.macros_title);
            var add = v.FindViewById<Button>(Resource.Id.macros_add);

            list.Adapter = new Adapters.MacrosAdapter(Activity, _macrosManager);
            text.Visibility = (_macrosManager.Macros.Count == 0) ? ViewStates.Visible : ViewStates.Gone;
            add.Click += Add_Click;

            return v;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            _popup = new FragmentPopupValue(new Models.ListCellModel(null, 
                                                Resource.String.popup_title_title, 
                                                Resource.String.popup_description_title,
                                                "", Android.Text.InputTypes.TextFlagAutoCorrect, 
                                                "", 0));
            _popup.PopupClose += PopupClose;
            _popup.Show(Activity.SupportFragmentManager, "PopupAddMacro");
        }

        private void PopupClose(object sender, Events.PopupEventArgs e)
        {
            if(e.HasResult == true)
            {
                var macro = new Models.MacroModel()
                {
                    Enabled = true,
                    Name = (e.Result as Models.ListCellModel).Value
                };
                _macrosManager.Macros.Add(macro);
            }
        }
    }
}