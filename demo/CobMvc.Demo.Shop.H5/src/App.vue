<template>
  <div id="app">
    <ProductList/>

    <hr>

    <AddressList/>
  </div>
</template>

<script>
import ProductList from './components/product/List.vue'
import AddressList from './components/user/address.vue'

export default {
  name: 'app',
  components: {
    ProductList,
    AddressList
  }
}
</script>

<style>
#app {
  font-family: 'Avenir', Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 60px;
}
</style>
