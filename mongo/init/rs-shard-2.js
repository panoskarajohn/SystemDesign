try {
  rs.status();
} catch (e) {
  rs.initiate({
    _id: "rs-shard-2",
    members: [{ _id: 0, host: "shard2:27017" }]
  });
}
