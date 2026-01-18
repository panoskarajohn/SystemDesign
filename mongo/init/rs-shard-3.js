try {
  rs.status();
} catch (e) {
  rs.initiate({
    _id: "rs-shard-3",
    members: [{ _id: 0, host: "shard3:27017" }]
  });
}
