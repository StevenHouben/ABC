﻿using System;
using System.Collections.Generic;
using ABC.Model.Device;
using ABC.Model.Users;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ABC.Infrastructure.Events;
using ABC.Infrastructure.Helpers;
using ABC.Model;


namespace ABC.Infrastructure.ActivityBase
{
    public class ActivityClient : ActivityController
    {
        #region Members

        readonly Connection _eventHandler;
        string Address { get; set; }
        bool _connected;

        #endregion


        #region Constructor/Destructor

        public ActivityClient( string ip, int port, IDevice device )
        {
            Ip = ip;
            Port = port;

            Address = Net.GetUrl( ip, port, "" ).ToString();

            Device = device;

            _eventHandler = new Connection( Address );
            _eventHandler.Received += eventHandler_Received;
            _eventHandler.Start().Wait();

            Initialize();
        }

        ~ActivityClient()
        {
            if ( _connected )
                RemoveDevice( Device.Id );

            _eventHandler.Stop();
        }

        #endregion


        #region Private Members

        void Initialize()
        {
            var acts = GetActivities();

            foreach ( var item in acts )
                activities.AddOrUpdate( item.Id, item, ( key, oldValue ) => item );

            var usrs = GetUsers();
            foreach ( var item in usrs )
                users.AddOrUpdate( item.Id, item, ( key, oldValue ) => item );

            var dvs = GetDevices();
            foreach ( var item in dvs )
                devices.AddOrUpdate( item.Id, item, ( key, oldValue ) => item );

            _connected = true;
        }

        void eventHandler_Received( string obj )
        {
            if ( obj == "Connected" )
            {
                Device.ConnectionId = _eventHandler.ConnectionId;
                AddDevice( Device );
                OnConnectionEstablished();
                return;
            }
            var content = JsonConvert.DeserializeObject<JObject>( obj );
            var eventType = content[ "Event" ].ToString();
            var data = content[ "Data" ].ToString();

            switch ( (NotificationType)Enum.Parse( typeof( NotificationType ), eventType ) )
            {
                case NotificationType.ActivityAdded:
                    OnActivityAdded( new ActivityEventArgs( Json.ConvertFromTypedJson<IActivity>( data ) ) );
                    break;
                case NotificationType.ActivityChanged:
                    OnActivityChanged( new ActivityEventArgs( Json.ConvertFromTypedJson<IActivity>( data ) ) );
                    break;
                case NotificationType.ActivityRemoved:
                    OnActivityRemoved(
                        new ActivityRemovedEventArgs(
                            JsonConvert.DeserializeObject<JObject>( data )[ "Id" ].ToString() ) );
                    break;
                case NotificationType.UserAdded:
                    OnUserAdded( new UserEventArgs( Json.ConvertFromTypedJson<IUser>( data ) ) );
                    break;
                case NotificationType.UserChanged:
                    OnUserAdded( new UserEventArgs( Json.ConvertFromTypedJson<IUser>( data ) ) );
                    break;
                case NotificationType.UserRemoved:
                    OnActivityRemoved(
                        new ActivityRemovedEventArgs(
                            JsonConvert.DeserializeObject<JObject>( data )[ "Id" ].ToString() ) );
                    break;
            }
        }

        #endregion


        #region Public Members

        public override void AddActivity( IActivity activity )
        {
            Rest.Post( Address + Url.Activities, activity );
        }

        public override void AddUser( IUser user )
        {
            Rest.Post( Address + Url.Users, user );
        }

        public override void RemoveUser( string id )
        {
            Rest.Delete( Address + Url.Users, id );
        }

        public override void UpdateUser( IUser user )
        {
            Rest.Post( Address + Url.Users, user );
        }

        public override IUser GetUser( string id )
        {
            return Json.ConvertFromTypedJson<IUser>( Rest.Get( Address + Url.Users, id ) );
        }

        public override void UpdateActivity( IActivity act )
        {
            Rest.Post( Address + Url.Activities, act );
        }

        public override void RemoveActivity( string id )
        {
            Rest.Delete( Address + Url.Activities, id );
        }

        public override IActivity GetActivity( string id )
        {
            return Json.ConvertFromTypedJson<IActivity>( Rest.Get( Address + Url.Activities, id ) );
        }

        public override List<IActivity> GetActivities()
        {
            return Json.ConvertArrayFromTypedJson<IActivity>( Rest.Get( Address + Url.Activities, "" ) );
        }

        public override void AddDevice( IDevice dev )
        {
            Rest.Post( Address + Url.Devices, dev );
        }

        public override void UpdateDevice( IDevice dev )
        {
            Rest.Post( Address + Url.Devices, dev );
        }

        public override void RemoveDevice( string id )
        {
            Rest.Delete( Address + Url.Devices, id );
        }

        public override IDevice GetDevice( string id )
        {
            return Json.ConvertFromTypedJson<IDevice>( Rest.Get( Address + Url.Devices, id ) );
        }

        public Type NotifierType { get; set; }

        public override List<IUser> GetUsers()
        {
            return Json.ConvertArrayFromTypedJson<IUser>( Rest.Get( Address + Url.Users, "" ) );
        }

        public override List<IDevice> GetDevices()
        {
            return Json.ConvertArrayFromTypedJson<IDevice>( Rest.Get( Address + Url.Devices, "" ) );
        }

        #endregion
    }

    public enum Url
    {
        Activities,
        Devices,
        Subscribers,
        Messages,
        Users,
        Files
    }
}