/*
 *  Managed C# wrapper for an extended version of ENet
 *  Copyright (c) 2013 James Bellinger
 *  Copyright (c) 2016 Nate Shoffner
 *  Copyright (c) 2018 Stanislav Denisov
 *  Copyright (c) 2019 Alexander Bondarev
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("Mirasrael.ENet.Tests")]

namespace ENet
{
	using System.Security;

	[Flags]
	public enum PacketFlags {
		None = 0,
		Reliable = 1 << 0,
		Unsequenced = 1 << 1,
		NoAllocate = 1 << 2,
		UnreliableFragmented = 1 << 3,
		Instant = 1 << 4,
		Unthrottled = 1 << 5,
		Sent =  1 << 8
	}

	public enum EventType {
		None = 0,
		Connect = 1,
		Disconnect = 2,
		Receive = 3,
		Timeout = 4
	}

	public enum PeerState {
		Uninitialized = -1,
		Disconnected = 0,
		Connecting = 1,
		AcknowledgingConnect = 2,
		ConnectionPending = 3,
		ConnectionSucceeded = 4,
		Connected = 5,
		DisconnectLater = 6,
		Disconnecting = 7,
		AcknowledgingDisconnect = 8,
		Zombie = 9
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ENetEvent {
		public EventType type;
		public IntPtr peer;
		public byte channelID;
		public uint data;
		public IntPtr packet;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ENetCallbacks {
		public AllocCallback malloc;
		public FreeCallback free;
		public NoMemoryCallback noMemory;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr AllocCallback(IntPtr size);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void FreeCallback(IntPtr memory);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void NoMemoryCallback();
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void PacketFreeCallback(Packet packet);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int InterceptCallback(ref Event @event, IntPtr address, IntPtr receivedData, int receivedDataLength);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate ulong ChecksumCallback(IntPtr buffers, int bufferCount);

	internal static class ArrayPool {
		[ThreadStatic]
		private static byte[] byteBuffer;
		[ThreadStatic]
		private static IntPtr[] pointerBuffer;

		public static byte[] GetByteBuffer() {
			if (byteBuffer == null)
				byteBuffer = new byte[64];

			return byteBuffer;
		}

		public static IntPtr[] GetPointerBuffer() {
			if (pointerBuffer == null)
				pointerBuffer = new IntPtr[Library.maxPeers];

			return pointerBuffer;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe ref struct Address {
		private fixed byte   ip[16];
		public ushort Port;

		/// <summary> Any IP address for IPv6. </summary>
		public static Address Any => new Address();

		/// <summary> Any IP address for IPv4. </summary>
		public static Address AnyV4
		{
			get
			{
				// ReSharper disable once UseObjectOrCollectionInitializer (can't be used with fixed array)
				var address = new Address();
				address.ip[10] = 0xff;
				address.ip[11] = 0xff;
				return address;
			}
		}

		public string GetIP() {
			StringBuilder ip = new StringBuilder(1025);

			if (Native.enet_address_get_ip(ref this, ip, (IntPtr)ip.Capacity) != 0)
				return String.Empty;

			return ip.ToString();
		}

		public bool SetIP(string ip) {
			if (ip == null)
				throw new ArgumentNullException("ip");

			return Native.enet_address_set_ip(ref this, ip) == 0;
		}

		public string GetHost() {
			StringBuilder hostName = new StringBuilder(1025);

			if (Native.enet_address_get_hostname(ref this, hostName, (IntPtr)hostName.Capacity) != 0)
				return String.Empty;

			return hostName.ToString();
		}

		public bool SetHost(string hostName) {
			if (hostName == null)
				throw new ArgumentNullException("hostName");

			return Native.enet_address_set_hostname(ref this, hostName) == 0;
		}
	}

	public struct Event {
		private ENetEvent nativeEvent;

		internal ENetEvent NativeData {
			get {
				return nativeEvent;
			}

			set {
				nativeEvent = value;
			}
		}

		internal Event(ENetEvent @event) {
			nativeEvent = @event;
		}

		public EventType Type {
			get {
				return nativeEvent.type;
			}
		}

		public Peer Peer {
			get {
				return new Peer(nativeEvent.peer);
			}
		}

		public byte ChannelID {
			get {
				return nativeEvent.channelID;
			}
		}

		public uint Data {
			get {
				return nativeEvent.data;
			}
		}

		public Packet Packet {
			get {
				return new Packet(nativeEvent.packet);
			}
		}
	}

	public class Callbacks {
		private ENetCallbacks nativeCallbacks;

		internal ENetCallbacks NativeData {
			get {
				return nativeCallbacks;
			}

			set {
				nativeCallbacks = value;
			}
		}

		public Callbacks(AllocCallback allocCallback, FreeCallback freeCallback, NoMemoryCallback noMemoryCallback) {
			nativeCallbacks.malloc = allocCallback;
			nativeCallbacks.free = freeCallback;
			nativeCallbacks.noMemory = noMemoryCallback;
		}
	}

	public struct Packet : IDisposable {
		private IntPtr nativePacket;

		internal IntPtr NativeData {
			get {
				return nativePacket;
			}

			set {
				nativePacket = value;
			}
		}

		internal Packet(IntPtr packet) {
			nativePacket = packet;
		}

		public void Dispose() {
			if (nativePacket != IntPtr.Zero) {
				Native.enet_packet_dispose(nativePacket);
				nativePacket = IntPtr.Zero;
			}
		}

		public bool IsSet {
			get {
				return nativePacket != IntPtr.Zero;
			}
		}

		public IntPtr Data {
			get {
				IsCreated();

				return Native.enet_packet_get_data(nativePacket);
			}
		}

		public IntPtr UserData {
			get {
				IsCreated();

				return Native.enet_packet_get_user_data(nativePacket);
			}

			set {
				IsCreated();

				Native.enet_packet_set_user_data(nativePacket, value);
			}
		}

		public int Length {
			get {
				IsCreated();

				return Native.enet_packet_get_length(nativePacket);
			}
		}

		public bool HasReferences {
			get {
				IsCreated();

				return Native.enet_packet_check_references(nativePacket) != 0;
			}
		}

		internal void IsCreated() {
			if (nativePacket == IntPtr.Zero)
				throw new InvalidOperationException("Packet not created");
		}

		public void SetFreeCallback(IntPtr callback) {
			IsCreated();

			Native.enet_packet_set_free_callback(nativePacket, callback);
		}

		public void SetFreeCallback(PacketFreeCallback callback) {
			IsCreated();

			Native.enet_packet_set_free_callback(nativePacket, Marshal.GetFunctionPointerForDelegate(callback));
		}

		public void Create(byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");

			Create(data, data.Length);
		}

		public void Create(byte[] data, int length) {
			Create(data, length, PacketFlags.None);
		}

		public void Create(byte[] data, PacketFlags flags) {
			Create(data, data.Length, flags);
		}

		public void Create(byte[] data, int length, PacketFlags flags) {
			if (data == null)
				throw new ArgumentNullException("data");

			if (length < 0 || length > data.Length)
				throw new ArgumentOutOfRangeException("length");

			nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
		}

		public void Create(IntPtr data, int length, PacketFlags flags) {
			if (data == IntPtr.Zero)
				throw new ArgumentNullException("data");

			if (length < 0)
				throw new ArgumentOutOfRangeException("length");

			nativePacket = Native.enet_packet_create(data, (IntPtr)length, flags);
		}

		public void Create(byte[] data, int offset, int length, PacketFlags flags) {
			if (data == null)
				throw new ArgumentNullException("data");

			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (length < 0 || length > data.Length)
				throw new ArgumentOutOfRangeException("length");

			nativePacket = Native.enet_packet_create_offset(data, (IntPtr)length, (IntPtr)offset, flags);
		}

		public void Create(IntPtr data, int offset, int length, PacketFlags flags) {
			if (data == IntPtr.Zero)
				throw new ArgumentNullException("data");

			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (length < 0)
				throw new ArgumentOutOfRangeException("length");

			nativePacket = Native.enet_packet_create_offset(data, (IntPtr)length, (IntPtr)offset, flags);
		}

		public void CopyTo(byte[] destination) {
			if (destination == null)
				throw new ArgumentNullException("destination");

			Marshal.Copy(Data, destination, 0, Length);
		}
	}

	public class Host : IDisposable {
		private static          InterceptCallback    overridenInterceptCallback;

		private readonly InterceptCallback interceptCallback;

		private IntPtr                       nativeHost;
		private event RawDataReceivedHandler rawDataReceivedEvent;

		public delegate void RawDataReceivedHandler(IntPtr address, IntPtr data, int length, ref bool consumed);

		public Host() { this.interceptCallback = this.Intercept; }

		public event RawDataReceivedHandler RawDataReceived
		{
			add
			{
				this.IsCreated();
				if (this.rawDataReceivedEvent == null)
					this.SetInterceptCallback(overridenInterceptCallback ?? this.interceptCallback);
				this.rawDataReceivedEvent += value;
			}
			remove
			{
				if (this.rawDataReceivedEvent == null) return;
				this.rawDataReceivedEvent -= value;
				if (this.rawDataReceivedEvent == null)
					this.SetInterceptCallback(IntPtr.Zero);
			}
		}

		internal IntPtr NativeData {
			get {
				return nativeHost;
			}

			set {
				nativeHost = value;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (nativeHost != IntPtr.Zero) {
				Native.enet_host_destroy(nativeHost);
				nativeHost = IntPtr.Zero;
			}
		}

		~Host() {
			Dispose(false);
		}

		public bool IsSet {
			get {
				return nativeHost != IntPtr.Zero;
			}
		}

		public uint PeersCount {
			get {
				IsCreated();

				return Native.enet_host_get_peers_count(nativeHost);
			}
		}

		public uint PacketsSent {
			get {
				IsCreated();

				return Native.enet_host_get_packets_sent(nativeHost);
			}
		}

		public uint PacketsReceived {
			get {
				IsCreated();

				return Native.enet_host_get_packets_received(nativeHost);
			}
		}

		public uint BytesSent {
			get {
				IsCreated();

				return Native.enet_host_get_bytes_sent(nativeHost);
			}
		}

		public uint BytesReceived {
			get {
				IsCreated();

				return Native.enet_host_get_bytes_received(nativeHost);
			}
		}

		public Address SocketAddress
		{
			get
			{
				this.IsCreated();

				var address = default(Address);
				Native.enet_host_get_socket_address(this.nativeHost, ref address);
				return address;
			}
		}

		internal void IsCreated() {
			if (nativeHost == IntPtr.Zero)
				throw new InvalidOperationException("Host not created");
		}

		private static void IsChannelsLimited(int channelLimit) {
			if (channelLimit < 0 || channelLimit > Library.maxChannelCount)
				throw new ArgumentOutOfRangeException("channelLimit");
		}

		public void Create() {
			Create(Address.Any, 1, 0);
		}

		public void Create(int bufferSize) {
			Create(Address.Any, 1, 0, 0, 0, bufferSize);
		}

		public void Create(Address address, int peerLimit) {
			Create(address, peerLimit, 0);
		}

		public void Create(Address address, int peerLimit, int channelLimit) {
			Create(address, peerLimit, channelLimit, 0, 0, 0);
		}

		public void Create(int peerLimit, int channelLimit) {
			Create(Address.Any, peerLimit, channelLimit, 0, 0, 0);
		}

		public void Create(int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth) {
			Create(Address.Any, peerLimit, channelLimit, incomingBandwidth, outgoingBandwidth, 0);
		}

		public void Create(Address address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth) {
			Create(address, peerLimit, channelLimit, incomingBandwidth, outgoingBandwidth, 0);
		}

		public void Create(Address address, int peerLimit, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize) {
			if (nativeHost != IntPtr.Zero)
				throw new InvalidOperationException("Host already created");

			if (peerLimit < 0 || peerLimit > Library.maxPeers)
				throw new ArgumentOutOfRangeException("peerLimit");

			IsChannelsLimited(channelLimit);

			nativeHost = Native.enet_host_create(ref address, (IntPtr)peerLimit, (IntPtr)channelLimit, incomingBandwidth, outgoingBandwidth, bufferSize);

			if (nativeHost == IntPtr.Zero)
				throw new InvalidOperationException("Host creation call failed");
		}

		public int SendRaw(Address address, byte[] data, int offset, int length)
		{
			return Native.enet_host_send_raw_ex(this.nativeHost, ref address, data, (IntPtr)offset, (IntPtr)length);
		}

		public void PreventConnections(bool state) {
			IsCreated();

			Native.enet_host_prevent_connections(nativeHost, (byte)(state ? 1 : 0));
		}

		public void Broadcast(byte channelID, ref Packet packet) {
			IsCreated();

			packet.IsCreated();
			Native.enet_host_broadcast(nativeHost, channelID, packet.NativeData);
			packet.NativeData = IntPtr.Zero;
		}

		public void Broadcast(byte channelID, ref Packet packet, Peer excludedPeer) {
			IsCreated();

			packet.IsCreated();
			Native.enet_host_broadcast_exclude(nativeHost, channelID, packet.NativeData, excludedPeer.NativeData);
			packet.NativeData = IntPtr.Zero;
		}

		public void Broadcast(byte channelID, ref Packet packet, Peer[] peers) {
			if (peers == null)
				throw new ArgumentNullException("peers");

			IsCreated();

			packet.IsCreated();

			if (peers.Length > 0) {
				IntPtr[] nativePeers = ArrayPool.GetPointerBuffer();
				int nativeCount = 0;

				for (int i = 0; i < peers.Length; i++) {
					if (peers[i].NativeData != IntPtr.Zero) {
						nativePeers[nativeCount] = peers[i].NativeData;
						nativeCount++;
					}
				}

				Native.enet_host_broadcast_selective(nativeHost, channelID, packet.NativeData, nativePeers, (IntPtr)nativeCount);
			}

			packet.NativeData = IntPtr.Zero;
		}

		public int CheckEvents(out Event @event) {
			IsCreated();

			ENetEvent nativeEvent;

			var result = Native.enet_host_check_events(nativeHost, out nativeEvent);

			if (result <= 0) {
				@event = default(Event);

				return result;
			}

			@event = new Event(nativeEvent);

			return result;
		}

		public Peer Connect(Address address) {
			return Connect(address, 0, 0);
		}

		public Peer Connect(Address address, int channelLimit) {
			return Connect(address, channelLimit, 0);
		}

		public Peer Connect(Address address, int channelLimit, uint data) {
			IsCreated();
			IsChannelsLimited(channelLimit);

			var peer = new Peer(Native.enet_host_connect(nativeHost, ref address, (IntPtr)channelLimit, data));

			if (peer.NativeData == IntPtr.Zero)
				throw new InvalidOperationException("Host connect call failed");

			return peer;
		}

		public int Service(int timeout, out Event @event) {
			if (timeout < 0)
				throw new ArgumentOutOfRangeException("timeout");

			IsCreated();

			ENetEvent nativeEvent;

			var result = Native.enet_host_service(nativeHost, out nativeEvent, (uint)timeout);

			if (result <= 0) {
				@event = default(Event);

				return result;
			}

			@event = new Event(nativeEvent);

			return result;
		}

		public void SetBandwidthLimit(uint incomingBandwidth, uint outgoingBandwidth) {
			IsCreated();

			Native.enet_host_bandwidth_limit(nativeHost, incomingBandwidth, outgoingBandwidth);
		}

		public void SetChannelLimit(int channelLimit) {
			IsCreated();
			IsChannelsLimited(channelLimit);

			Native.enet_host_channel_limit(nativeHost, (IntPtr)channelLimit);
		}

		public void SetMaxDuplicatePeers(ushort number) {
			IsCreated();

			Native.enet_host_set_max_duplicate_peers(nativeHost, number);
		}

		public void SetInterceptCallback(IntPtr callback) {
			IsCreated();

			Native.enet_host_set_intercept_callback(nativeHost, callback);
		}

		public void SetInterceptCallback(InterceptCallback callback) {
			IsCreated();

			Native.enet_host_set_intercept_callback(nativeHost, Marshal.GetFunctionPointerForDelegate(callback));
		}

		public void SetChecksumCallback(IntPtr callback) {
			IsCreated();

			Native.enet_host_set_checksum_callback(nativeHost, callback);
		}

		public void SetChecksumCallback(ChecksumCallback callback) {
			IsCreated();

			Native.enet_host_set_checksum_callback(nativeHost, Marshal.GetFunctionPointerForDelegate(callback));
		}

		public void Flush() {
			IsCreated();

			Native.enet_host_flush(nativeHost);
		}

		public uint MTU => Native.enet_host_get_mtu(this.nativeHost);

		public static int HandleIntercept(ref Event @event, IntPtr receivedAddress, IntPtr receivedData, int receivedDataLength)
		{
			throw new NotSupportedException();
		}

		private int Intercept(ref Event @event, IntPtr receivedAddress, IntPtr receivedData, int receivedDataLength)
		{
			var consumed = false;
			this.rawDataReceivedEvent?.Invoke(receivedAddress, receivedData, receivedDataLength, ref consumed);
			return consumed ? 1 : 0;
		}

		/// <summary>
		/// In some scenarios you may need override default Intercept callback (i.e. in Unity you need to set special annotation in IL2CPP for callback from native code).
		/// You then can call <see cref="HandleIntercept"/> to use override callback as wrapper.
		/// You must call this method before first Host created.
		/// </summary>
		/// <param name="interceptCallback"></param>
		public static void OverrideInterceptCallback(InterceptCallback interceptCallback)
		{
			overridenInterceptCallback = interceptCallback;
		}
	}

	public struct Peer {
		private IntPtr nativePeer;
		private uint nativeID;

		internal IntPtr NativeData {
			get {
				return nativePeer;
			}

			set {
				nativePeer = value;
			}
		}

		internal Peer(IntPtr peer) {
			nativePeer = peer;
			nativeID = nativePeer != IntPtr.Zero ? Native.enet_peer_get_id(nativePeer) : 0;
		}

		public bool IsSet {
			get {
				return nativePeer != IntPtr.Zero;
			}
		}

		public uint ID {
			get {
				return nativeID;
			}
		}

		public string IP {
			get {
				IsCreated();

				byte[] ip = ArrayPool.GetByteBuffer();

				if (Native.enet_peer_get_ip(nativePeer, ip, (IntPtr)ip.Length) == 0)
					return Encoding.ASCII.GetString(ip, 0, ip.StringLength());
				else
					return String.Empty;
			}
		}

		public ushort Port {
			get {
				IsCreated();

				return Native.enet_peer_get_port(nativePeer);
			}
		}

		public uint MTU {
			get {
				IsCreated();

				return Native.enet_peer_get_mtu(nativePeer);
			}
		}

		public PeerState State {
			get {
				return nativePeer == IntPtr.Zero ? PeerState.Uninitialized : Native.enet_peer_get_state(nativePeer);
			}
		}

		public uint RoundTripTime {
			get {
				IsCreated();

				return Native.enet_peer_get_rtt(nativePeer);
			}
		}

		public uint LastRoundTripTime {
			get {
				IsCreated();

				return Native.enet_peer_get_last_rtt(nativePeer);
			}
		}

		public uint LastSendTime {
			get {
				IsCreated();

				return Native.enet_peer_get_lastsendtime(nativePeer);
			}
		}

		public uint LastReceiveTime {
			get {
				IsCreated();

				return Native.enet_peer_get_lastreceivetime(nativePeer);
			}
		}

		public ulong PacketsSent {
			get {
				IsCreated();

				return Native.enet_peer_get_packets_sent(nativePeer);
			}
		}

		public ulong PacketsLost {
			get {
				IsCreated();

				return Native.enet_peer_get_packets_lost(nativePeer);
			}
		}

		public float PacketsThrottle {
			get {
				IsCreated();

				return Native.enet_peer_get_packets_throttle(nativePeer);
			}
		}

		public ulong BytesSent {
			get {
				IsCreated();

				return Native.enet_peer_get_bytes_sent(nativePeer);
			}
		}

		public ulong BytesReceived {
			get {
				IsCreated();

				return Native.enet_peer_get_bytes_received(nativePeer);
			}
		}

		public IntPtr Data {
			get {
				IsCreated();

				return Native.enet_peer_get_data(nativePeer);
			}

			set {
				IsCreated();

				Native.enet_peer_set_data(nativePeer, value);
			}
		}

		internal void IsCreated() {
			if (nativePeer == IntPtr.Zero)
				throw new InvalidOperationException("Peer not created");
		}

		public void ConfigureThrottle(uint interval, uint acceleration, uint deceleration, uint threshold) {
			IsCreated();

			Native.enet_peer_throttle_configure(nativePeer, interval, acceleration, deceleration, threshold);
		}

		public bool Send(byte channelID, ref Packet packet) {
			IsCreated();

			packet.IsCreated();

			return Native.enet_peer_send(nativePeer, channelID, packet.NativeData) == 0;
		}

		public bool Receive(out byte channelID, out Packet packet) {
			IsCreated();

			IntPtr nativePacket = Native.enet_peer_receive(nativePeer, out channelID);

			if (nativePacket != IntPtr.Zero) {
				packet = new Packet(nativePacket);

				return true;
			}

			packet = default(Packet);

			return false;
		}

		public void Ping() {
			IsCreated();

			Native.enet_peer_ping(nativePeer);
		}

		public void PingInterval(uint interval) {
			IsCreated();

			Native.enet_peer_ping_interval(nativePeer, interval);
		}

		public void Timeout(uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum) {
			IsCreated();

			Native.enet_peer_timeout(nativePeer, timeoutLimit, timeoutMinimum, timeoutMaximum);
		}

		public void Disconnect(uint data) {
			IsCreated();

			Native.enet_peer_disconnect(nativePeer, data);
		}

		public void DisconnectNow(uint data) {
			IsCreated();

			Native.enet_peer_disconnect_now(nativePeer, data);
		}

		public void DisconnectLater(uint data) {
			IsCreated();

			Native.enet_peer_disconnect_later(nativePeer, data);
		}

		public void Reset() {
			IsCreated();

			Native.enet_peer_reset(nativePeer);
		}
	}

	public static class Extensions {
		public static int StringLength(this byte[] data) {
			if (data == null)
				throw new ArgumentNullException("data");

			int i;

			for (i = 0; i < data.Length && data[i] != 0; i++);

			return i;
		}
	}

	public static class Library {
		public const uint maxChannelCount = 0xFF;
		public const uint maxPeers = 0xFFF;
		public const uint maxPacketSize = 32 * 1024 * 1024;
		public const uint throttleThreshold = 40;
		public const uint throttleScale = 32;
		public const uint throttleAcceleration = 2;
		public const uint throttleDeceleration = 2;
		public const uint throttleInterval = 5000;
		public const uint timeoutLimit = 32;
		public const uint timeoutMinimum = 5000;
		public const uint timeoutMaximum = 30000;
		public const uint version = (2 << 16) | (4 << 8) | (3);

		public static uint Time {
			get {
				return Native.enet_time_get();
			}
		}

		public static bool Initialize() {
			if (Native.enet_linked_version() != version)
				throw new InvalidOperationException("Incompatatible version");

			return Native.enet_initialize() == 0;
		}

		public static bool Initialize(Callbacks callbacks) {
			if (callbacks == null)
				throw new ArgumentNullException("callbacks");

			if (Native.enet_linked_version() != version)
				throw new InvalidOperationException("Incompatible version");

			ENetCallbacks nativeCallbacks = callbacks.NativeData;

			return Native.enet_initialize_with_callbacks(version, ref nativeCallbacks) == 0;
		}

		public static void Deinitialize() {
			Native.enet_deinitialize();
		}

		public static ulong CRC64(IntPtr buffers, int bufferCount) {
			return Native.enet_crc64(buffers, bufferCount);
		}
	}

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
        internal static extern int enet_initialize_with_callbacks(uint version, ref ENetCallbacks inits);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_deinitialize();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_linked_version();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_time_get();

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_crc64(IntPtr buffers, int bufferCount);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_ip(ref Address address, string ip);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_set_hostname(ref Address address, string hostName);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_ip(ref Address address, StringBuilder ip, IntPtr ipLength);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_address_get_hostname(ref Address address, StringBuilder hostName, IntPtr nameLength);

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
        internal static extern IntPtr enet_packet_get_user_data(IntPtr packet);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr enet_packet_set_user_data(IntPtr packet, IntPtr userData);

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
        internal static extern void enet_host_set_max_duplicate_peers(IntPtr host, ushort number);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_set_intercept_callback(IntPtr host, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_set_checksum_callback(IntPtr host, IntPtr callback);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_host_get_mtu(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int enet_host_get_socket_address(IntPtr host, ref Address address);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_flush(IntPtr host);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void enet_host_destroy(IntPtr host);

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
        internal static extern uint enet_peer_get_last_rtt(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastsendtime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint enet_peer_get_lastreceivetime(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_sent(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong enet_peer_get_packets_lost(IntPtr peer);

        [DllImport(nativeLibrary, CallingConvention = CallingConvention.Cdecl)]
        internal static extern float enet_peer_get_packets_throttle(IntPtr peer);

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