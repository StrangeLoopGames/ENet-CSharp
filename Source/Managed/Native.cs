namespace ENet
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    [SuppressUnmanagedCodeSecurity]
    internal static class Native {
#if __IOS__ || UNITY_IOS && !UNITY_EDITOR
        private const string nativeLibrary = "__Internal";
#else
        private const string nativeLibrary = "enet";
#endif

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_initialize_with_callbacks(uint version, ENetCallbacks inits);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_deinitialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_time_get();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host_ip(ref Address address, string ip);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_host(ref Address address, string hostName);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host_ip(ref Address address, StringBuilder ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_host(ref Address address, StringBuilder hostName, IntPtr nameLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(byte[] data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create(IntPtr data, IntPtr dataLength, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create_offset(byte[] data, IntPtr dataLength, IntPtr dataOffset, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_create_offset(IntPtr data, IntPtr dataLength, IntPtr dataOffset, PacketFlags flags);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_check_references(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_get_data(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_packet_get_length(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_set_free_callback(IntPtr packet, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_packet_dispose(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(ref Address address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_create(IntPtr address, IntPtr peerLimit, IntPtr channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_connect(IntPtr host, ref Address address, IntPtr channelCount, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_send_raw(IntPtr host, ref Address address, byte[] data, /*size_t*/ IntPtr dataLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_send_raw_ex(IntPtr host, ref Address address, byte[] buffer, /*size_t*/ IntPtr offset, /*size_t*/ IntPtr length);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_set_intercept(IntPtr host, ENetInterceptCallback callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_broadcast(IntPtr host, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast_exclude(IntPtr host, byte channelID, IntPtr packet, IntPtr excludedPeer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_broadcast_selective(IntPtr host, byte channelID, IntPtr packet, IntPtr[] peers, IntPtr peersLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_service(IntPtr host, out ENetEvent @event, uint timeout);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_check_events(IntPtr host, out ENetEvent @event);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_channel_limit(IntPtr host, IntPtr channelLimit);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_bandwidth_limit(IntPtr host, uint incomingBandwidth, uint outgoingBandwidth);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_peers_count(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_packets_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_sent(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_bytes_received(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_mtu(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_received_data(IntPtr host, out IntPtr receivedData);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_host_get_received_address_ptr(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_get_socket_address(IntPtr host, ref Address address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_flush(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_destroy(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_enable_compression(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_prevent_connections(IntPtr host, byte state);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_throttle_configure(IntPtr peer, uint interval, uint acceleration, uint deceleration, uint threshold);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_id(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_get_ip(IntPtr peer, byte[] ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort enet_peer_get_port(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_mtu(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern PeerState enet_peer_get_state(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_rtt(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastsendtime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastreceivetime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_sent(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_lost(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_bytes_sent(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_bytes_received(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_get_data(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_set_data(IntPtr peer, IntPtr data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_peer_send(IntPtr peer, byte channelID, IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_peer_receive(IntPtr peer, out byte channelID);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_ping_interval(IntPtr peer, uint pingInterval);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_timeout(IntPtr peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_now(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_disconnect_later(IntPtr peer, uint data);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_peer_reset(IntPtr peer);
    }
}