try {
  rs.status();
} catch (e) {
  rs.initiate({
    _id: "rs-shard-1",
    members: [{ _id: 0, host: "shard1:27017" }]
  });
}
