/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class ToolbarButtonElement : Button
    {
        VisualElement root;
        
        private bool toggleState;
        public bool Toggle
        {
            set
            {
                toggleState = value;
                if (toggleState)
                {
                    root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                }
                else
                {
                    root.style.backgroundColor = TileWorldCreatorColor.Grey.GetColor();
                }
            }
            get
            {
                return toggleState;
            }
        }

        public ToolbarButtonElement(Vector2Int _size, string _title, Texture2D _icon, bool _isToggle = false)
        {
            root = new VisualElement();
            root.style.width = _size.x;
            root.style.height = _size.y;
            root.style.backgroundColor = new Color(0, 0, 0, 0);

            root.SetBorder(1);

            if (!string.IsNullOrEmpty(_title))
            { 
                var _label = new Label(_title);

                root.Add(_label);
            }

            if (_icon != null)
            {
                var _iconElement = new VisualElement();
                _iconElement.style.backgroundImage = _icon;
                _iconElement.style.width = _size.x - 8;
                _iconElement.style.height = _size.y - 8;
                _iconElement.SetMargin(4, 4, 4, 4);
                root.Add(_iconElement);
            }

            root.RegisterCallback<MouseEnterEvent>(e => root.style.backgroundColor = TileWorldCreatorColor.LightGrey.GetColor());
            root.RegisterCallback<MouseLeaveEvent>(e => 
            {
                if (!_isToggle)
                {
                    root.style.backgroundColor = TileWorldCreatorColor.Grey.GetColor();
                }
                else
                {
                    if (!toggleState)
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.Grey.GetColor();
                    }
                    else
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                    }
                }
            });
            root.RegisterCallback<ClickEvent>(e => 
            {
                if (_isToggle)
                {
                    toggleState = !toggleState;
                    if (toggleState)
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                    }
                    else
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.LightGrey.GetColor();
                    }
                }
            });

            this.Add(root);
        }

    }
}
#endif