﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using UForms.Core;

namespace UForms.Controls
{
    public class Control : IDrawable
    {
        public enum VisibilityMode
        {
            Visible,
            Hidden,
            Collapsed
        }

        // Dirty flag should be used to trigger a repaint on internal component changes, as otherwise repaint will only be invoked by specific editor events
        // flag will propagate upwards and will be collected by the application from the root component if it reaches it.
        public bool Dirty
        {
            get { return m_dirty; }
            set 
            {
                m_dirty = value;

                if ( value && m_container != null )
                {
                    m_container.Dirty = value;
                }
            }
        }

        public Rect Bounds        
        {
            get             { return m_bounds; }
            private set     { m_bounds = value; } 
        }

        // Final computed screen rect taking into account screen position, margins and size
        public Rect ScreenRect
        {
            get         { return m_screenRect; }
            private set { m_screenRect = value; }
        }

        // Cached parent's screen position so deep elements don't have to traverse all the way up
        public Vector2 ParentScreenPosition         
        {
            get             { return m_screenPosition; } 
            set             { m_screenPosition = value; } 
        }

        // Position is used to hint to the control it's desired position on screen.
        public Vector2 Position 
        {
            get { return m_position; }
            set { m_position = value; } 
        }

        // Size is used to hint to the control it's desired size on screen.
        public Vector2 Size 
        {
            get { return m_size; }
            set { m_size = value; } 
        }           
     
        public Vector2 MarginLeftTop
        {
            get { return m_marginLeftTop; }
            set { m_marginLeftTop = value; }
        }

        public Vector2 MarginRightBottom
        {
            get { return m_marginRightBottom; }
            set { m_marginRightBottom = value; }
        }

        // Is this control enabled? this property will propagate to all child contorls and can be applied to interactive controls as well as containers
        public bool Enabled
        {
            get;
            set;
        }

        // What is the visibility mode of the control?
        // Visible = default
        // Hidden = layout control and reserve space but don't draw
        // Collapsed = don't layout, generate empty rect and don't draw
        public VisibilityMode    Visibility
        {
            get { return m_visibility; }
            set { m_visibility = value; }
        }


        // Default size for this control
        protected virtual Vector2 DefaultSize
        {
            get { return Vector2.zero; }
        }

        public      List<Control>     Children      { get; private set; }        // Contained children elements.               
        
        protected   Control           m_container;                               // Containing element if control is in a hierarchy.

        private     Rect              m_bounds;
        private     Rect              m_screenRect;

        private     Vector2           m_screenPosition;

        private     Vector2           m_position;
        private     Vector2           m_size;

        private     Vector2           m_marginLeftTop;
        private     Vector2           m_marginRightBottom;

        private     VisibilityMode    m_visibility;

        private     bool              m_dirty;

        #region Internal Drawing Events

        protected virtual void OnBeforeDraw() { }
        protected virtual void OnDraw() { }
        protected virtual void OnLayout() { }
        protected virtual void OnAfterLayout() { }
        #endregion

        #region Internal System Events

        protected virtual void OnContextClick( Event e ) { }
        protected virtual void OnDragExited( Event e ) { }
        protected virtual void OnDragPerform( Event e ) { }
        protected virtual void OnDragUpdated( Event e ) { }
        protected virtual void OnExecuteCommand( Event e ) { }
        protected virtual void OnIgnore( Event e ) { }
        protected virtual void OnKeyDown( Event e ) { }
        protected virtual void OnKeyUp( Event e ) { }
        protected virtual void OnLayout( Event e ) { }
        protected virtual void OnMouseDown( Event e ) { }
        protected virtual void OnMouseDrag( Event e ) { }
        protected virtual void OnMouseMove( Event e ) { }
        protected virtual void OnMouseUp( Event e ) { }
        protected virtual void OnRepaint( Event e ) { }
        protected virtual void OnScrollWheel( Event e ) { }
        protected virtual void OnUsed( Event e ) { }
        protected virtual void OnValidateCommand( Event e ) { }

        #endregion


        public Control()
        {
            Children    = new List<Control>();

            Position    = Vector2.zero;
            Size        = DefaultSize;

            Enabled     = true;
            Visibility  = VisibilityMode.Visible;
        }


        public Control( Vector2 position, Vector2 size )
        {
            Children    = new List<Control>();

            Position    = position;
            Size        = size;

            Enabled     = true;
            Visibility  = VisibilityMode.Visible;
        }

        public void AddChild( Control child )
        {
            child.m_container = this;

            Children.Add( child );
        }


        public void RemoveChild( Control child )
        {
            if ( child.m_container == this )
            {
                child.m_container = null;

                Children.Remove( child );
            }
        }

        public Control SetMargin( float left, float top, float right, float bottom )
        {
            MarginLeftTop       = new Vector2( left, top );
            MarginRightBottom   = new Vector2( right, bottom );

            return this;
        }


        public Control SetSize ( float x, float y )
        {
            return SetSize( new Vector2( x, y ) );
        }


        public Control SetSize( Vector2 size )
        {
            Size = size;
            return this;
        }


        public Control SetPosition( float x, float y )
        {
            return SetPosition( new Vector2( x, y ) );
        }


        public Control SetPosition( Vector2 position )
        {
            Position = position;
            return this;
        }

        // Returns the content bounds rectangle without factoring the Size property
        public Rect GetContentBounds()
        {
            Rect r = new Rect( 0, 0, 0, 0 );

            foreach ( Control child in Children )
            {
                r.x      = Mathf.Min( r.x, child.Position.x );
                r.y      = Mathf.Min( r.y, child.Position.y );
                r.width  = Mathf.Max( r.width, child.Visibility == VisibilityMode.Collapsed ? 0.0f : child.Position.x + child.Size.x );
                r.height = Mathf.Max( r.height, child.Visibility == VisibilityMode.Collapsed ? 0.0f : child.Position.y + child.Size.y );
            }

            return r;
        }

        public void Layout()
        {
            // Cache parent screen position
            if ( m_container == null )
            {
                ParentScreenPosition = Vector2.zero;
            }
            else
            {
                ParentScreenPosition = m_container.ParentScreenPosition + m_container.Position;
            }

            ScreenRect = new Rect(
                ParentScreenPosition.x + Position.x + MarginLeftTop.x,
                ParentScreenPosition.y + Position.y + MarginLeftTop.y,
                Size.x - MarginLeftTop.x - MarginRightBottom.x,
                Size.y - MarginLeftTop.y - MarginRightBottom.y
            );

            OnLayout();

            if ( m_visibility != VisibilityMode.Collapsed )
            {
                foreach( Control child in Children )
                {
                    child.Layout();
                }

                // Update bounds
                float x = 0.0f;
                float y = 0.0f;
                float w = Size.x;
                float h = Size.y;

                Rect content = GetContentBounds();
                x = Mathf.Min( x, content.x );
                y = Mathf.Min( y, content.y );
                w = Mathf.Max( w, content.width );
                h = Mathf.Max( h, content.height );

                x += Position.x;
                y += Position.y;

                Bounds = new Rect( x, y, w, h );
            }
            else
            {
                Bounds = new Rect( Position.x, Position.y, 0.0f, 0.0f );
            }           

            OnAfterLayout();
        }

        public void Draw()
        {            
            // We will cache the enabled property at the beginning of the draw phase so we can safely determine if we should close it as soon as drawing completes.
            // This is a fail safe in case the state changes while drawing is being processed (one example would be buttons invoking actions if clicked immediately, inside the draw call).
            bool localScopeEnabled = Enabled;
            if ( !localScopeEnabled )
            {
                EditorGUI.BeginDisabledGroup( true );
            }

            if ( m_visibility == VisibilityMode.Visible )
            {
                OnBeforeDraw();

                foreach ( Control child in Children )
                {
                    child.Draw();
                }

                OnDraw();
            }                        

            if ( !localScopeEnabled )
            {
                EditorGUI.EndDisabledGroup();
            }
        }


        public void ProcessEvents( Event e )
        {
            if ( e == null )
            {
                return;
            }

            switch ( e.type )
            {
                case EventType.ContextClick:
                OnContextClick( e );
                break;

                case EventType.DragExited:
                OnDragExited( e );
                break;

                case EventType.DragPerform:
                OnDragPerform( e );
                break;

                case EventType.DragUpdated:
                OnDragUpdated( e );
                break;

                case EventType.ExecuteCommand:
                OnExecuteCommand( e );
                break;

                case EventType.Ignore:
                OnIgnore( e );
                break;

                case EventType.KeyDown:
                OnKeyDown( e );
                break;

                case EventType.KeyUp:
                OnKeyUp( e );
                break;

                case EventType.Layout:
                OnLayout( e );
                break;

                case EventType.MouseDown:
                OnMouseDown( e );
                break;

                case EventType.MouseDrag:
                OnMouseDrag( e );
                break;

                case EventType.MouseMove:
                OnMouseMove( e );
                break;

                case EventType.MouseUp:
                OnMouseUp( e );
                break;

                case EventType.Repaint:
                OnRepaint( e );
                break;

                case EventType.ScrollWheel:
                OnScrollWheel( e );
                break;

                case EventType.Used:
                OnUsed( e );
                break;

                case EventType.ValidateCommand:
                OnValidateCommand( e );
                break;
            }

            foreach( Control child in Children )
            {
                // If event was consumed during propagation, stop processing
                if ( e == null )
                {
                    return;
                }

                child.ProcessEvents( e );
            }
        }
    }
}